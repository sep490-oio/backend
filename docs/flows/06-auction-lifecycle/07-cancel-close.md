# 07 - Cancel & Close Auction

## Overview

Two separate endpoints allow ending an auction early:
- **Cancel**: Seller/admin explicitly cancels with a reason. Item returns to Active status, all scheduled jobs are removed, and bids are cancelled.
- **Close**: Seller/admin triggers the normal end-resolution pipeline (same as `EndAuctionCommand`) to end the auction early with a winner if one exists.

---

## Endpoints

| Operation | Method | URL | Permission | Response |
|---|---|---|---|---|
| Cancel | `POST` | `api/auctions/{auctionId}/cancel` | `Catalogs.Auctions.Cancel` | `204 No Content` |
| Close | `POST` | `api/auctions/{auctionId}/close` | `Catalogs.Auctions.Cancel` | `200 OK` |

---

## Cancel Auction

### Request Body

```json
{
  "reason": "string (required)"
}
```

### CancelAuctionCommand Handler Flow

1. Load the auction with `Bids`, `AutoBids`, `PriceHistories`, `Watchers`, `Item` (split query).
2. **Authorization**: Caller must be the item seller (`auction.Item.SellerId == currentUser.UserId`) or have `App.Roles.Catalogs.Admin`.
3. Call `auction.CancelAuction(reason, nowUtc)`.
4. Load the `Item` by `auction.ItemId` and call `item.ReturnToActive(nowUtc)` to return the item to `Active` status.
5. Save changes via `_unitOfWork.SaveChangesAsync()`.
6. Cancel all scheduled Quartz jobs via `_scheduler.CancelAsync(auction.Id.Value)` -- this removes both the `ActivateAuctionJob` and `EndAuctionJob`.

### Domain Logic: Auction.CancelAuction()

```
Guard: Status.CanTransitionTo(AuctionStatus.Cancelled) -- must be true
Status = AuctionStatus.Cancelled
ActualEndTime = nowUtc
```

Side effects:
- All bids with `BidStatus.Active` or `BidStatus.Winning` are cancelled via `bid.Cancel()`.
- All auto-bids are terminalized via `TerminalizeAllAutoBids(nowUtc)`.
- Raises `AuctionCancelledEvent(AuctionId, Reason, OccurredAt)`.

### Scheduler Cancellation

`QuartzAuctionScheduler.CancelAsync()` deletes both scheduled jobs:
- `ActivateAuctionJob` key: `activate-{auctionId}`
- `EndAuctionJob` key: `end-{auctionId}`

**Source:** `src/core/OIO.Application/Context/AuctionContext/Commands/CancelAuction/CancelAuctionCommand.cs`

---

## Close Auction (Early End)

The Close endpoint sends an `EndAuctionCommand` -- the exact same command used by the `EndAuctionJob`. This triggers the full end-resolution pipeline documented in `06-end-resolve.md`.

```csharp
var result = await sender.Send(new EndAuctionCommand(auctionId), ct);
```

The handler flow is identical to the scheduled end:
1. Load auction, check authorization (seller or admin).
2. If sealed auction, decrypt and reveal all sealed bids.
3. Route through `AuctionGrain.EndAuctionAsync()`.
4. Execute `Auction.End()` then `Auction.Resolve()`.

The result is a normal end resolution: the auction may end as `Sold` or `Failed` depending on bids and reserve price.

**Source:** `src/presentation/OIO.Api/Endpoints/AuctionContext/Auctions/CloseAuctionEndpoint.cs`

---

## Domain Events

| Event | Trigger |
|---|---|
| `AuctionCancelledEvent` | `Auction.CancelAuction()` -- contains auction ID, cancellation reason, timestamp. |
| `AuctionEndedEvent` | Close path only (via `Auction.End()`) |
| `AuctionSoldEvent` / `AuctionFailedEvent` | Close path only (via `Auction.Resolve()`) |

---

## Error Codes

| Code | Type | Message |
|---|---|---|
| `Auction.NotFound` | NotFound | Auction with Id '{id}' was not found. |
| `Auction.OnlyOwnerCanCancel` | Forbidden | Only the auction owner can cancel. |
| `Auction.InvalidState` | Conflict | Cannot perform 'cancel' when auction status is '{currentState}'. |
| `Auction.OnlyOwnerCanClose` | Forbidden | Only the auction owner or an admin can close this auction. |

---

## Key Source Files

| File | Path |
|---|---|
| CancelAuctionCommand | `src/core/OIO.Application/Context/AuctionContext/Commands/CancelAuction/CancelAuctionCommand.cs` |
| CancelAuctionEndpoint | `src/presentation/OIO.Api/Endpoints/AuctionContext/Auctions/CancelAuctionEndpoint.cs` |
| CloseAuctionEndpoint | `src/presentation/OIO.Api/Endpoints/AuctionContext/Auctions/CloseAuctionEndpoint.cs` |
| EndAuctionCommand | `src/core/OIO.Application/Context/AuctionContext/Commands/EndAuction/EndAuctionCommand.cs` |
| Auction aggregate | `src/core/OIO.Domain/Context/AuctionContext/Aggregates/Auctions/Auction.cs` |
| QuartzAuctionScheduler | `src/infrastructure/OIO.Infrastructure/Scheduling/QuartzAuctionScheduler.cs` |
