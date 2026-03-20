# 02 - Payment Callback (VNPay IPN)

## Callback Decision Tree

```mermaid
---
config:
  layout: elk
---
flowchart TD
    IPN[VNPay IPN Callback] --> Parse[Parse & validate signature]
    Parse --> FindTx[Find Transaction by TxnRef]
    FindTx --> Idempotent{Already processed?<br/>Status = Completed or Failed}
    Idempotent -->|Yes| ReturnExisting[Return existing result]
    Idempotent -->|No| ResolvePurpose[ResolvePaymentPurpose]
    ResolvePurpose --> IsBuyNow{purpose == AuctionBuyNow?<br/>transaction.BuyNowReservationId.HasValue}
    IsBuyNow -->|No| OtherFlow[Route to deposit/order/wallet handler]
    IsBuyNow -->|Yes| VNPaySuccess{VNPay IsSuccess?}

    VNPaySuccess -->|No| FailPayment[HandleAuctionBuyNowFailedAsync]
    FailPayment --> FailReservation1[auction.FailBuyNowReservation<br/>reason = payment_failed]
    FailReservation1 --> MarkTxFailed[Transaction.MarkAsFailed]
    MarkTxFailed --> SaveFail[SaveChangesAsync]

    VNPaySuccess -->|Yes| LoadReservation[Load reservation + auction + buyer]
    LoadReservation --> IsActive{reservation.IsActive now?<br/>PendingPayment AND ExpiresAt > now}

    IsActive -->|No| LatePath[CreditLateBuyNowPaymentToWalletAsync<br/>full transaction.Amount to buyer wallet]
    LatePath --> FailLate[auction.FailBuyNowReservation<br/>reason = late_payment_success]
    FailLate --> MarkTxCompleted1[Transaction.MarkAsCompleted]
    MarkTxCompleted1 --> SaveLate[SaveChangesAsync]

    IsActive -->|Yes| CreateOrder[CreateBuyNowOrder]
    CreateOrder --> OrderOK{Order created OK?}
    OrderOK -->|No| CreditWallet2[CreditLateBuyNowPaymentToWalletAsync]
    CreditWallet2 --> FailOrder[FailBuyNowReservation<br/>reason = order_creation_failed_after_payment]
    FailOrder --> MarkTxCompleted4[Transaction.MarkAsCompleted]
    MarkTxCompleted4 --> SaveOrderFail[SaveChangesAsync]

    OrderOK -->|Yes| Finalize[auction.FinalizeBuyNowReservation]
    Finalize --> FinalizeOK{Finalize OK?}
    FinalizeOK -->|No| CreditWallet3[CreditLateBuyNowPaymentToWalletAsync]
    CreditWallet3 --> FailFinalize[FailBuyNowReservation<br/>reason = buy_now_finalize_failed_after_payment]
    FailFinalize --> MarkTxCompleted5[Transaction.MarkAsCompleted]
    MarkTxCompleted5 --> SaveFinalizeFail[SaveChangesAsync]

    FinalizeOK -->|Yes| InsertOrder[dbContext.Insert Order]
    InsertOrder --> LinkOrder[auction.LinkBuyNowReservationOrder]
    LinkOrder --> CreateEscrow{transaction.Amount > 0?}
    CreateEscrow -->|Yes| Escrow[Escrow.Create for gateway amount]
    CreateEscrow -->|No| DepositCheck
    Escrow --> DepositCheck{reservation.DepositAppliedAmount > 0?}
    DepositCheck -->|Yes| ApplyDeposit[ApplyBuyNowDepositFundingAsync]
    DepositCheck -->|No| MarkPaid
    ApplyDeposit --> MarkPaid[order.MarkAsPaid]
    MarkPaid --> MarkTxCompleted2[Transaction.MarkAsCompleted]
    MarkTxCompleted2 --> TokenLink[TryLinkOrCreatePaymentMethodFromTokenAsync]
    TokenLink --> SaveSuccess[SaveChangesAsync]
```

---

## Routing

The `ProcessVnPayCallbackCommandHandler` resolves payment purpose via `ResolvePaymentPurpose`:

```
if (transaction.BuyNowReservationId.HasValue)  →  PaymentPurpose.AuctionBuyNow
```

When `purpose == PaymentPurpose.AuctionBuyNow`:
- **Success callback** (`callback.IsSuccess == true`): routes to `HandleAuctionBuyNowAsync`.
- **Failed callback** (`callback.IsSuccess == false`): routes to `HandleAuctionBuyNowFailedAsync`.

---

## Normal Path (Happy Flow)

When VNPay payment succeeds and the reservation is still active (`PendingPayment` + `ExpiresAt > now`):

| Step | Operation | Detail |
|------|-----------|--------|
| 1 | Load reservation | Query by `BuyNowReservationId` or `PaymentTransactionId`, include Auction with Bids, AutoBids, Deposits, Participants, BuyNowReservations, Item |
| 2 | Load buyer | With Profile and Addresses |
| 3 | `CreateBuyNowOrder` | Creates `Order.Create(...)` with buyer shipping snapshot, `OrderPricing` at buy-now price, zero fees |
| 4 | `auction.FinalizeBuyNowReservation` | Cancels active/winning bids, creates winning bid, marks reservation Paid, sets auction to Sold |
| 5 | `dbContext.Insert(order)` | Persists the new order |
| 6 | `auction.LinkBuyNowReservationOrder` | Sets `reservation.OrderId = order.Id` |
| 7 | `Escrow.Create` | Creates escrow for the gateway payment amount (if `transaction.Amount > 0`) |
| 8 | `ApplyBuyNowDepositFundingAsync` | If `reservation.DepositAppliedAmount > 0`: converts deposit, debits wallet pending balance, creates second escrow |
| 9 | `order.MarkAsPaid` | Transitions order to Paid status |
| 10 | `transaction.MarkAsCompleted` | Marks VNPay transaction as completed with gateway info |
| 11 | `TryLinkOrCreatePaymentMethodFromTokenAsync` | Best-effort: auto-create/update PaymentMethod from VNPay token |
| 12 | `SaveChangesAsync` | Single unit of work commit |

---

## Late Payment Path

When VNPay payment succeeds but `reservation.IsActive(now) == false` (expired):

1. `CreditLateBuyNowPaymentToWalletAsync`: credits full `transaction.Amount` to buyer wallet.
2. `auction.FailBuyNowReservation(reservationId, "late_payment_success")`: marks reservation as Failed.
3. No order created. Auction remains in its current state.

See [04-late-payment.md](./04-late-payment.md) for details.

---

## Payment Failure Path

When VNPay reports `IsSuccess == false`:

1. `transaction.MarkAsFailed(gatewayInfo, now)` -- marks the transaction as failed.
2. `HandleAuctionBuyNowFailedAsync`:
   - Loads reservation by `BuyNowReservationId` or `PaymentTransactionId`.
   - Calls `auction.FailBuyNowReservation(reservationId, "payment_failed", now)`.
   - Reservation status becomes `Failed`.
   - Raises `AuctionBuyNowReservationReleasedEvent` with reason `"payment_failed"`.
3. `SaveChangesAsync`.

The auction is unlocked and other buyers can initiate new reservations.

---

## Error Handling with Wallet Credit Fallback

After a successful VNPay payment, if any subsequent step fails (order creation, finalization), the handler:

1. Credits the full `transaction.Amount` to the buyer's wallet via `CreditLateBuyNowPaymentToWalletAsync`.
2. Fails the reservation with a descriptive reason.
3. The buyer retains funds in their wallet for future use.

This avoids the complexity of VNPay refund processing. The three failure-after-payment scenarios:

| Failure Point | Reservation Failure Reason |
|---------------|---------------------------|
| `CreateBuyNowOrder` fails | `order_creation_failed_after_payment` |
| `FinalizeBuyNowReservation` fails | `buy_now_finalize_failed_after_payment` |
| Reservation expired (late payment) | `late_payment_success` |
