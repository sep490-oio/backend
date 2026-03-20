# Buy Now

## Overview

The Buy Now flow allows a qualified buyer to purchase an auction item immediately at a fixed
price, bypassing the competitive bidding process. It creates a time-limited **reservation**,
generates a VNPay payment URL, and -- upon successful payment callback -- finalises the
purchase by creating an order and marking the auction as **Sold**.

---

## Sequence: Reserve, Pay, Finalise

```mermaid
sequenceDiagram
    participant Buyer
    participant API as REST / SignalR
    participant Handler as BuyNowCommandHandler
    participant Grain as AuctionGrain
    participant Auction as Auction Aggregate
    participant VNPay as VNPay Gateway
    participant Callback as ProcessVnPayCallbackHandler

    Buyer->>API: POST /api/auctions/{id}/buy-now<br/>(or hub.BuyNow)
    API->>Handler: BuyNowCommand(auctionId, ipAddress)
    Handler->>Grain: InitiateBuyNowReservationAsync(buyerId, 15 min)
    Grain->>Auction: InitiateBuyNowReservation(buyerId, nowUtc, 15 min)

    Note over Auction: Validate: BuyNow available,<br/>no active reservation,<br/>not seller, status=Scheduled,<br/>qualification open
    Note over Auction: Deposit auto-apply:<br/>appliedDeposit = min(heldDeposit, buyNowPrice)<br/>amountDue = buyNowPrice - appliedDeposit

    Auction-->>Grain: AuctionBuyNowReservation
    Grain-->>Handler: AuctionBuyNowReservationGrain

    Handler->>VNPay: CreateVnPayPaymentUrlCommand(amountDue, BuyNowReservationId)
    VNPay-->>Handler: { PaymentUrl, TransactionId }

    alt Payment URL creation fails
        Handler->>Grain: FailBuyNowReservationAsync(reservationId, "payment_url_creation_failed")
        Handler-->>Buyer: Error
    end

    Handler->>Grain: AttachBuyNowPaymentAsync(reservationId, transactionId)

    alt Attach fails
        Handler->>Grain: FailBuyNowReservationAsync(reservationId, "payment_transaction_attach_failed")
        Handler-->>Buyer: Error
    end

    Handler-->>Buyer: BuyNowCheckoutDto (paymentUrl, expiresAt, amounts)

    Buyer->>VNPay: Redirect to paymentUrl & complete payment
    VNPay->>Callback: IPN / Return callback

    alt Payment succeeds & reservation still active
        Callback->>Auction: FinalizeBuyNowReservation(reservationId)
        Note over Auction: Cancel all active/winning bids,<br/>create winning Bid,<br/>mark reservation Paid,<br/>set auction status = Sold
        Callback->>Callback: Create Order, Escrow, apply deposit funding
        Callback->>Auction: LinkBuyNowReservationOrder(reservationId, orderId)
        Callback->>Auction: order.MarkAsPaid()
    end

    alt Payment succeeds but reservation expired (late payment)
        Callback->>Callback: CreditLateBuyNowPaymentToWalletAsync (wallet credit)
        Callback->>Auction: FailBuyNowReservation("late_payment_success")
    end

    alt Payment fails
        Callback->>Auction: FailBuyNowReservation("payment_failed")
    end
```

---

## BuyNowReservation State Machine

```mermaid
---
config:
  layout: elk
---
stateDiagram-v2
    [*] --> PendingPayment : Create reservation

    PendingPayment --> Paid : VNPay callback success<br/>(reservation still active)
    PendingPayment --> Expired : ExpireBuyNowReservationsJob<br/>(expiresAt <= now)
    PendingPayment --> Failed : Payment URL creation failed<br/>/ payment failed<br/>/ late payment success<br/>/ finalize failed
    PendingPayment --> Cancelled : Manual cancel

    Paid --> [*]
    Expired --> [*]
    Failed --> [*]
    Cancelled --> [*]

    note right of PendingPayment
        Only non-terminal state.
        All transitions require
        current status = PendingPayment.
    end note
```

---

## Endpoints

### REST

| Method | Path | Auth | Idempotent | Success | Errors |
|--------|------|------|------------|---------|--------|
| `POST` | `/api/auctions/{auctionId}/buy-now` | `Auctions.BuyNow` permission | Yes (via `IdempotencyFilter`, cache key `buy-now:idempotency:{userId}:{auctionId}`) | `201 Created` | `400`, `409` |

### SignalR

| Method | Signature | Returns |
|--------|-----------|---------|
| `BuyNow` | `BuyNow(auctionId: Guid)` | `HubCommandResult<BuyNowCheckoutDto>` |

---

## Response: `BuyNowCheckoutDto`

```csharp
public sealed record BuyNowCheckoutDto(
    Guid ReservationId,
    string PaymentUrl,
    DateTime ExpiresAt,
    MoneyDto BuyNowPrice,
    MoneyDto DepositAppliedAmount,
    MoneyDto AmountDue);
```

| Field | Description |
|-------|-------------|
| `ReservationId` | Unique ID of the `AuctionBuyNowReservation` |
| `PaymentUrl` | VNPay redirect URL for the buyer |
| `ExpiresAt` | UTC timestamp when the reservation expires (now + 15 min) |
| `BuyNowPrice` | Full buy-now price of the auction |
| `DepositAppliedAmount` | Portion of the held deposit auto-applied: `min(heldDeposit, buyNowPrice)` |
| `AmountDue` | Amount the buyer must pay via VNPay gateway: `buyNowPrice - depositApplied` |

---

## 15-Minute Reservation Window

The `BuyNowCommandHandler` hard-codes the reservation window:

```csharp
private static readonly TimeSpan ReservationWindow = TimeSpan.FromMinutes(15);
```

The reservation is created with `ExpiresAt = nowUtc + 15 minutes`. During this window:

- **New bids are blocked.** `EnsureNotLockedByBuyNowReservation` returns
  `Auction.BuyNowReservationActive` if an active reservation exists.
- The buyer must complete VNPay payment before expiry.

---

## Deposit Auto-Apply

When initiating a buy-now reservation, the domain automatically applies the buyer's held
deposit toward the buy-now price:

```
heldDeposit   = deposits.FirstOrDefault(d => d.BidderId == buyerId && d.IsHeld)?.Amount ?? 0
appliedDeposit = min(heldDeposit.Amount, buyNowPrice.Amount)
amountDue      = buyNowPrice.Amount - appliedDeposit
```

The `AuctionBuyNowReservation.Create` factory validates:

```
depositAppliedAmount + gatewayAmountDue == buyNowPrice
```

If this invariant fails, the error `AuctionBuyNowReservation.InvalidFundingSplit` is returned.

---

## VNPay URL Creation

The handler sends a `CreateVnPayPaymentUrlCommand` with:

| Parameter | Value |
|-----------|-------|
| `Amount` | `reservation.GatewayAmountDue.Amount` |
| `Currency` | `reservation.GatewayAmountDue.Currency` |
| `Purpose` | `PaymentPurpose.AuctionBuyNow` |
| `IpAddress` | Caller IP from HTTP context |
| `Description` | `"AuctionBuyNow - Auction #{auctionId}"` |
| `AuctionId` | Auction ID |
| `BuyNowReservationId` | Reservation ID |

---

## Auction Lock During Reservation

While an `AuctionBuyNowReservation` with status `PendingPayment` and `ExpiresAt > now` exists,
the auction is **locked**:

- `PlaceBid` calls `EnsureNotLockedByBuyNowReservation` which checks
  `GetActiveBuyNowReservation(nowUtc)` and returns `Auction.BuyNowReservationActive` if a
  live reservation is found.
- `EndAuction` in the grain also checks `GetActiveBuyNowReservation` and skips ending if
  a reservation is active.

---

## ExpireBuyNowReservationsJob

| Property | Value |
|----------|-------|
| Type | `BackgroundService` |
| Interval | 1 minute (`TimeSpan.FromMinutes(1)`) |
| Batch size | 50 auctions per cycle (`.Take(50)`) |

**Logic per cycle:**

1. Query auctions with at least one `BuyNowReservation` where
   `Status == PendingPayment && ExpiresAt <= nowUtc`.
2. For each matching reservation, call `auction.ExpireBuyNowReservation(reservationId, nowUtc)`.
3. Save changes.
4. If the auction is still `Active`, its `EndTime` has passed, and no active reservation
   remains, send `EndAuctionCommand` to end the auction.

This ensures auctions whose end time elapsed during a reservation get properly closed once
the reservation expires.

---

## Late Payment Edge Case

If VNPay sends a success callback **after** the reservation has expired
(`!reservation.IsActive(now)`), the `ProcessVnPayCallbackHandler`:

1. **Credits** the payment amount to the buyer's wallet via
   `CreditLateBuyNowPaymentToWalletAsync`.
2. **Fails** the reservation with reason `"late_payment_success"`.
3. The auction remains unlocked and can proceed normally (e.g. end with a bidding winner).

The buyer receives a wallet credit instead of a completed purchase.

---

## Error Codes

| Code | HTTP | Description |
|------|------|-------------|
| `Auction.NotSupportBuyNow` | 409 | Auction does not have a buy-now price or it is unavailable |
| `Auction.BuyNowReservationActive` | 409 | Another buyer already has an active reservation |
| `Auction.SelfBid` | 403 | Seller cannot buy their own auction |
| `Auction.BuyNowUnavailableForScheduledAuction` | 409 | Buy now is only available during qualification window before auction starts |
| `Auction.TimingRequired` | 400 | Auction timing (start/end) not configured |
| `AuctionBuyNowReservation.InvalidFundingSplit` | 400 | Deposit + gateway amount does not equal buy-now price |
| `AuctionBuyNowReservation.InvalidExpiration` | 400 | Reservation expiration must be in the future |
| `AuctionBuyNowReservation.InvalidState` | 409 | Cannot perform action on reservation in current status |
| `AuctionBuyNowReservation.Expired` | 409 | Reservation has already expired |
| `AuctionBuyNowReservation.NotFound` | 404 | Reservation not found |
