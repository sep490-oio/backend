# 03 - Callback Processing (ProcessVnPayCallbackCommand)

## Overview

`ProcessVnPayCallbackCommand` is the core handler that processes VNPay payment results. It validates the callback signature, finds the matching Transaction, resolves the payment purpose, and executes purpose-specific business logic (deposit, order payment, buy-now, or wallet top-up).

**Source**: `OIO.Application/Context/PaymentContext/Commands/ProcessVnPayCallback/ProcessVnPayCallbackCommand.cs`

---

## Processing Flow

```mermaid
---
config:
  layout: elk
---
flowchart TD
    Start([ProcessVnPayCallbackCommand]) --> ValidateSig[_paymentGateway.ProcessCallback<br/>Validate HMAC-SHA512 signature<br/>Parse vnp_TxnRef, vnp_ResponseCode,<br/>vnp_Token, vnp_CardNumber, etc.]
    ValidateSig -->|Invalid| ErrSig[Error: InvalidSignature]
    ValidateSig -->|Valid| FindTxn[Find Transaction by<br/>TransactionNumber == callback.TransactionRef]
    FindTxn -->|Not found| ErrNotFound[Error: Transaction.NotFound]
    FindTxn -->|Found| IdempotencyCheck{Status already<br/>Completed or Failed?}
    IdempotencyCheck -->|Yes| ReturnCached[Return existing result<br/>IsSuccess = status == Completed]
    IdempotencyCheck -->|No| ResolvePurpose[ResolvePaymentPurpose]

    ResolvePurpose --> PurposeLogic{Resolved Purpose}

    PurposeLogic -->|AuctionBuyNow<br/>BuyNowReservationId set| CheckSuccess1{callback.IsSuccess?}
    PurposeLogic -->|AuctionDeposit<br/>AuctionId + Type=Deposit| CheckSuccess2{callback.IsSuccess?}
    PurposeLogic -->|OrderPayment<br/>OrderId set| CheckSuccess3{callback.IsSuccess?}
    PurposeLogic -->|WalletTopUp<br/>fallback| CheckSuccess4{callback.IsSuccess?}

    CheckSuccess1 -->|Yes| HandleBuyNow[HandleAuctionBuyNowAsync]
    CheckSuccess1 -->|No| HandleFailed1[HandleFailedCallback<br/>+ FailBuyNowReservation]

    CheckSuccess2 -->|Yes| HandleDeposit[HandleAuctionDepositAsync]
    CheckSuccess2 -->|No| HandleFailed2[HandleFailedCallback]

    CheckSuccess3 -->|Yes| HandleOrder[HandleOrderPaymentAsync]
    CheckSuccess3 -->|No| HandleFailed3[HandleFailedCallback]

    CheckSuccess4 -->|Yes| HandleTopUp[HandleWalletTopUpAsync]
    CheckSuccess4 -->|No| HandleFailed4[HandleFailedCallback]

    HandleDeposit --> DepositSteps[1. Load Auction with Deposits + Participants<br/>2. Validate auction state + qual window<br/>3. wallet.Credit amount<br/>4. wallet.Hold amount<br/>5. AuctionDeposit.Create<br/>6. auction.RegisterParticipantFromDeposit]

    HandleOrder --> OrderSteps[1. Load Order<br/>2. Detect hybrid wallet hold<br/>3. Escrow.Create for full amount<br/>4. wallet.DebitPending hybrid hold<br/>5. Convert winner deposit if exists<br/>6. order.MarkAsPaid]

    HandleBuyNow --> BuyNowCheck{reservation.IsActive?}
    BuyNowCheck -->|Yes| BuyNowActive[1. CreateBuyNowOrder<br/>2. auction.FinalizeBuyNowReservation<br/>3. Insert Order<br/>4. auction.LinkBuyNowReservationOrder<br/>5. Escrow.Create for gateway amount<br/>6. Apply deposit funding if applicable<br/>7. order.MarkAsPaid]
    BuyNowCheck -->|No / Expired| BuyNowLate[1. CreditLateBuyNowPaymentToWallet<br/>2. auction.FailBuyNowReservation<br/>reason: late_payment_success]

    HandleTopUp --> TopUpSteps[1. Load Wallet<br/>2. wallet.Credit amount]

    DepositSteps --> MarkCompleted
    OrderSteps --> MarkCompleted
    BuyNowActive --> MarkCompleted
    BuyNowLate --> MarkCompleted
    TopUpSteps --> MarkCompleted

    MarkCompleted[transaction.MarkAsCompleted<br/>gatewayInfo, now]
    MarkCompleted --> AutoLink[TryLinkOrCreatePaymentMethodFromTokenAsync<br/>best-effort, catch all exceptions]
    AutoLink --> SaveAll[_unitOfWork.SaveChangesAsync]
    SaveAll --> SuccessResponse([ProcessVnPayCallbackResponse<br/>IsSuccess: true])

    HandleFailed1 --> MarkFailed
    HandleFailed2 --> MarkFailed
    HandleFailed3 --> MarkFailed
    HandleFailed4 --> MarkFailed
    MarkFailed[transaction.MarkAsFailed<br/>gatewayInfo, now]
    MarkFailed --> SaveFailed[_unitOfWork.SaveChangesAsync]
    SaveFailed --> FailResponse([ProcessVnPayCallbackResponse<br/>IsSuccess: false])
```

---

## Step-by-Step Processing

### 1. Parse and Validate

Calls `_paymentGateway.ProcessCallback(queryParams)` which:
- Validates HMAC-SHA512 signature via `VnPayHelper.ValidateSignature(queryParams, HashSecret)`
- Parses: `vnp_TxnRef`, `vnp_TransactionNo`, `vnp_Amount` (divided by 100), `vnp_ResponseCode`, `vnp_TransactionStatus`, `vnp_BankCode`, `vnp_CardType`, `vnp_PayDate`
- Parses token fields: `vnp_Token` (with case fallback `vnp_token`), `vnp_CardNumber` (with fallback `vnp_card_number`)
- Success determination: `ResponseCode == "00" && TransactionStatus == "00"`

### 2. Find Transaction

Queries `Transaction` where `TransactionNumber.Value == callback.TransactionRef`.

### 3. Idempotency Check

If `Transaction.Status` is already `Completed` or `Failed`, returns immediately with the existing result. This prevents duplicate processing when both IPN and Return paths fire.

### 4. Purpose Resolution

```csharp
private static PaymentPurpose ResolvePaymentPurpose(Transaction transaction)
{
    if (transaction.BuyNowReservationId.HasValue)
        return PaymentPurpose.AuctionBuyNow;

    if (transaction.AuctionId.HasValue && transaction.Type == TransactionType.Deposit)
        return PaymentPurpose.AuctionDeposit;

    if (transaction.OrderId.HasValue)
        return PaymentPurpose.OrderPayment;

    // Fallback: check Description prefix
    // "[AuctionDeposit]..." -> AuctionDeposit
    // "[AuctionBuyNow]..." -> AuctionBuyNow
    // "[OrderPayment]..."  -> OrderPayment
    // else -> WalletTopUp
}
```

Priority order: `BuyNowReservationId` > `AuctionId + Deposit type` > `OrderId` > Description prefix > fallback `WalletTopUp`.

---

## Purpose Handlers (Success Path)

### HandleAuctionDepositAsync

1. Require `transaction.AuctionId`
2. Load Auction with `Item`, `Deposits`, `Participants`
3. Validate: not seller, auction not in terminal state, qualification window open, no duplicate held deposit
4. Load user's Wallet
5. `wallet.Credit(amount, transactionId, description, now)` -- add funds from VNPay
6. `wallet.Hold(amount, transactionId, description, now)` -- hold funds for deposit
7. `AuctionDeposit.Create(auctionId, userId, amount, transactionId, now)`
8. `auction.RegisterParticipantFromDeposit(userId, now)` -- auto-register as participant
9. Insert AuctionDeposit entity

### HandleOrderPaymentAsync

1. Require `transaction.OrderId`
2. Load Order
3. **Detect hybrid wallet hold**: Load buyer's Wallet with WalletTransactions, find last `[HybridHold]` transaction containing the OrderId
4. **Create Escrow**: Amount = `transaction.Amount + walletHoldAmount` (full order amount for hybrid payments)
5. **Commit hybrid hold** (if present): `wallet.DebitPending(walletHoldAmount, transactionId, "[HybridHold] committed", now)`
6. **Convert winner deposit** (if exists): Find held `AuctionDeposit` for the buyer on this auction -> `deposit.ConvertToPayment(now)` -> `wallet.DebitPending(depositAmount, ...)`
7. `order.MarkAsPaid(now)`

### HandleAuctionBuyNowAsync

1. Load `AuctionBuyNowReservation` with Auction (+ Item, Bids, AutoBids, Deposits, Participants, BuyNowReservations)
2. Load Buyer (User with Profile, Addresses)
3. **Check if reservation is still active** (`reservation.IsActive(now)`)

**If active:**
1. `CreateBuyNowOrder()` -- builds Order from buyer's default address, reservation pricing
2. `auction.FinalizeBuyNowReservation(reservationId, now)`
3. Insert Order, link reservation to order
4. `Escrow.Create(orderId, transactionId, transaction.Amount, currency, now)` for the gateway payment
5. If `reservation.DepositAppliedAmount > 0`: call `ApplyBuyNowDepositFundingAsync` which creates a separate Transaction (`BNDEP-{guid}`), converts the deposit, debits wallet, and creates a second Escrow for the deposit portion
6. `order.MarkAsPaid(now)`

**If expired:**
1. `CreditLateBuyNowPaymentToWalletAsync` -- credits the payment amount to buyer's wallet
2. `auction.FailBuyNowReservation(reservationId, "late_payment_success", now)`

### HandleWalletTopUpAsync

1. Load user's Wallet
2. `wallet.Credit(amount, transactionId, description, now)`

---

## Failure Path

1. `transaction.MarkAsFailed(gatewayInfo, now)` -- raises `TransactionFailedDomainEvent`
2. **Buy-now specific**: If purpose is `AuctionBuyNow`, also calls `HandleAuctionBuyNowFailedAsync`:
   - Loads the reservation
   - `auction.FailBuyNowReservation(reservationId, "payment_failed", now)`
3. Save changes

---

## Auto-Link PaymentMethod from Token

After successful processing, `TryLinkOrCreatePaymentMethodFromTokenAsync` runs as **best-effort** (wrapped in try-catch, exceptions are logged but do not fail the payment):

1. If `callback.VnPayToken` is empty, skip
2. Search for existing `PaymentMethod` with same `UserId`, `Type=VnPay`, same `VnPayToken`, `IsActive`
3. **If found**: `existing.UpdateVnPayToken(token, maskedCard, cardType, bankCode)` + `transaction.AssociatePaymentMethod(existing.Id)`
4. **If not found**: `PaymentMethod.CreateFromVnPayToken(...)` with `IsDefault=false`, insert, then `transaction.AssociatePaymentMethod(newPM.Id)`

---

## Response

```csharp
public sealed record ProcessVnPayCallbackResponse(
    string TransactionRef,
    bool IsSuccess,
    string ResponseCode,
    string Message);  // "Thanh toan thanh cong" or "Thanh toan that bai (ma: {code})"
```
