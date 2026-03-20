# Bidding Enums Reference

## Overview

This document catalogues the four status enums used across the bidding domain. Each enum
extends `EnumValueObject<T>` and is stored as a string identifier in the database.

---

## BidStatus

**Source:** `OIO.Domain.Context.AuctionContext.Enums.BidStatus`

Tracks the lifecycle of a single `Bid` entity.

| Value | String ID | Terminal | Description |
|-------|-----------|----------|-------------|
| `Active` | `active` | No | Bid is live and currently valid. Initial state on creation. |
| `Outbid` | `outbid` | Yes | A higher bid has been placed; this bid is no longer competitive. Set by `Bid.MarkAsOutbid()`. |
| `Winning` | `winning` | No | Bid is currently the highest bid. Set by `Bid.MarkAsWinning()`. |
| `Won` | `won` | Yes | Auction resolved and this bid is the winner. Set by `Bid.MarkAsWon()`. |
| `Cancelled` | `cancelled` | Yes | Bid has been cancelled (admin action or buy-now finalization). Set by `Bid.Cancel()`. |

### Transition Rules

```
Active --> Outbid       (higher bid placed)
Active --> Winning      (becomes highest bid)
Active --> Cancelled    (admin cancel or buy-now finalize)
Winning --> Outbid      (higher bid placed)
Winning --> Won         (auction resolved, this bid wins)
Winning --> Cancelled   (admin cancel or buy-now finalize)
```

---

## AutoBidStatus

**Source:** `OIO.Domain.Context.AuctionContext.Enums.AutoBidStatus`

Tracks the lifecycle of an `AutoBid` configuration.

| Value | String ID | Terminal | Description |
|-------|-----------|----------|-------------|
| `Active` | `active` | No | Auto-bid is enabled and will place bids automatically when outbid. Initial state on creation. |
| `Paused` | `paused` | No | Temporarily disabled by user. Wallet hold is retained for instant resume. |
| `Exhausted` | `exhausted` | No | Budget fully consumed (`Budget.IsExhausted`). Auto-reactivated if user increases max amount. |
| `Won` | `won` | Yes | Auction resolved and this auto-bidder won. Set by `AutoBid.MarkAsWon()`. |
| `Outbid` | `outbid` | No | Another bidder exceeded the max amount. Can be reactivated by updating config. |

### Transition Rules

```
Active --> Paused        (user pauses: AutoBid.Pause())
Active --> Exhausted     (budget consumed during bid placement)
Active --> Won           (auction resolved, auto-bidder wins)
Active --> Outbid        (outbid beyond max amount)

Paused --> Active        (user resumes: AutoBid.Resume())

Exhausted --> Active     (user increases max amount via UpdateConfig)

Outbid --> Active        (user increases max amount via UpdateConfig)

Won --> [terminal]       (cannot modify: CannotModifyFinalStatus)
```

**Key behaviors:**

- `Pause()` only allowed from `Active`. Sets `IsEnabled = false`, `StopReason = "paused_by_user"`.
- `Resume()` only allowed from `Paused`. Sets `IsEnabled = true`, clears stop reason.
- `UpdateConfig()` re-activates from `Exhausted` or `Outbid` back to `Active`. Blocked from `Won`.
- `MarkAsWon()` is the only true terminal state -- `UpdateConfig` is blocked by `CannotModifyFinalStatus`.

---

## SealedBidStatus

**Source:** `OIO.Domain.Context.AuctionContext.Enums.SealedBidStatus`

Tracks the lifecycle of a `SealedBid` in sealed-bid auctions.

| Value | String ID | Terminal | Description |
|-------|-----------|----------|-------------|
| `Submitted` | `submitted` | No | Bid has been submitted with an encrypted amount. Initial state. |
| `Revealed` | `revealed` | Yes | Bid amount has been decrypted and revealed (post-auction). Set by `SealedBid.Reveal()`. |
| `Invalidated` | `invalidated` | Yes | Bid was deemed invalid during reveal (e.g. decryption failure, below reserve). |
| `Withdrawn` | `withdrawn` | Yes | Bidder withdrew the sealed bid before reveal. |

### Transition Rules

```
Submitted --> Revealed       (admin/system reveals after auction ends)
Submitted --> Invalidated    (reveal fails validation)
Submitted --> Withdrawn      (bidder withdraws before reveal)

Revealed --> [terminal]
Invalidated --> [terminal]
Withdrawn --> [terminal]
```

---

## BuyNowReservationStatus

**Source:** `OIO.Domain.Context.AuctionContext.Enums.BuyNowReservationStatus`

Tracks the lifecycle of an `AuctionBuyNowReservation`.

| Value | String ID | Terminal | Description |
|-------|-----------|----------|-------------|
| `PendingPayment` | `pending_payment` | No | Reservation created, awaiting VNPay payment. Initial and only non-terminal state. |
| `Paid` | `paid` | Yes | Payment received and reservation finalized. Auction marked as `Sold`. |
| `Expired` | `expired` | Yes | 15-minute window elapsed without payment. Set by `ExpireBuyNowReservationsJob`. |
| `Cancelled` | `cancelled` | Yes | Reservation manually cancelled. `FailureReason` populated with cancel reason. |
| `Failed` | `failed` | Yes | Reservation failed due to system error (e.g. payment URL creation failed, late payment). `FailureReason` populated. |

### Transition Rules

```
PendingPayment --> Paid         (VNPay callback success, reservation still active)
PendingPayment --> Expired      (ExpireBuyNowReservationsJob: expiresAt <= now)
PendingPayment --> Cancelled    (manual cancel by user or system)
PendingPayment --> Failed       (payment_url_creation_failed / payment_failed /
                                 late_payment_success / payment_transaction_attach_failed /
                                 order_creation_failed_after_payment /
                                 buy_now_finalize_failed_after_payment)

Paid --> [terminal]
Expired --> [terminal]
Cancelled --> [terminal]
Failed --> [terminal]
```

### `IsTerminal` Property

```csharp
public bool IsTerminal =>
    this == Paid ||
    this == Expired ||
    this == Cancelled ||
    this == Failed;
```

All state transitions are guarded by checking `Status == PendingPayment`. Any attempt to
transition from a terminal state returns `AuctionBuyNowReservation.InvalidState`.

### Known Failure Reasons

| Reason string | Trigger |
|---------------|---------|
| `payment_url_creation_failed` | VNPay URL generation failed in `BuyNowCommandHandler` |
| `payment_transaction_attach_failed` | Failed to attach `TransactionId` to reservation |
| `payment_failed` | VNPay callback reported payment failure |
| `late_payment_success` | VNPay payment succeeded after reservation expired |
| `order_creation_failed_after_payment` | Order creation failed after successful payment |
| `buy_now_finalize_failed_after_payment` | `FinalizeBuyNowReservation` failed after payment |
| `expired` | Set as the `Reason` on the `AuctionBuyNowReservationReleasedEvent` when expired by job |
