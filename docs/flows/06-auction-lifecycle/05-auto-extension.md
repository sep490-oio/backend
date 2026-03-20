# 05 - Auto-Extension

## Overview

When a bid is placed near the end of an auction, the auction's end time is automatically extended to prevent sniping. This is controlled by the `TryAutoExtend()` private method on the `Auction` aggregate, which is called as part of every `PlaceBid()` invocation. The extension reschedules the `EndAuctionJob` via `IAuctionScheduler.RescheduleEndAsync()`.

**Sealed auctions do not support auto-extension.** The `autoExtend` flag is forced to `false` for sealed auctions -- setting it to `true` returns `Auction.SealedAutoExtendNotSupported`.

**Source files:**

| Concern | Path |
|---|---|
| Auction.TryAutoExtend() | `src/core/OIO.Domain/Context/AuctionContext/Aggregates/Auctions/Auction.cs` (line ~1789) |
| Auction.IsEndingSoon() | `src/core/OIO.Domain/Context/AuctionContext/Aggregates/Auctions/Auction.cs` (line ~1761) |
| AuctionExtendedEventHandler | `src/core/OIO.Application/Context/AuctionContext/EventHandlers/AuctionExtendedEventHandler.cs` |
| IAuctionScheduler | `src/core/OIO.Application/Abstractions/Scheduling/IAuctionScheduler.cs` |
| Config | `config/appsettings.Production.json` -- `Auction` section |

---

## Configuration

From `appsettings.Production.json` -> `Auction`:

| Key | Value | Type | Description |
|-----|-------|------|-------------|
| `ExtensionThreshold` | `00:05:00` | `TimeSpan` | A bid placed within this duration of EndTime triggers extension |
| `MaxExtensionsPerAuction` | `10` | `int` | Maximum number of times an auction can be extended |
| `MaxDuration` | `30.00:00:00` | `TimeSpan` | Absolute maximum total duration (30 days), extension cannot exceed this |

---

## TryAutoExtend() Logic

The method is called inside `PlaceBid()` after the bid is accepted and auction price is updated. It is a **private** method on the `Auction` aggregate.

### Preconditions (all must be true to extend)

```
Info is not null
  AND Info.AutoExtend == true
  AND Info.ExtensionCount < maxExtensions (10)
  AND IsEndingSoon(nowUtc, extensionThresholdMinutes)
```

If any condition is false, the method returns `UnitResult.Success` (no-op).

### IsEndingSoon()

```csharp
public bool IsEndingSoon(DateTime nowUtc, TimeSpan extensionThresholdMinutes) =>
    Status == AuctionStatus.Active &&
    Info is not null &&
    Info.RemainingTime(nowUtc) <= extensionThresholdMinutes;
```

This returns `true` when:
- Auction is `Active`.
- `Info` exists.
- Remaining time until `EndTime` is less than or equal to the threshold (5 minutes).

### Extension Execution

1. Save the old `EndTime`.
2. Call `Info.Extend(maxDuration)`:
   - Adds `ExtensionMinutes` to the current `EndTime`.
   - Increments `ExtensionCount`.
   - Respects the `maxDuration` ceiling.
   - Returns a new `AuctionInfo` value object (immutable).
3. Update `Info` with the new extended value.
4. Set `ModifiedAt`.
5. Raise `AuctionExtendedEvent`:
   - `AuctionId`
   - `TriggerByBidId` -- the bid that triggered the extension
   - `PreviousEndTime` -- old end time
   - `NewEndTime` -- new extended end time
   - `ExtensionMinutes` -- minutes added
   - `ExtensionCount` -- total extensions so far
   - `OccurredAt`

---

## AuctionExtendedEventHandler

Handles the `AuctionExtendedEvent`:

1. **Reschedule EndAuctionJob:** Calls `IAuctionScheduler.RescheduleEndAsync(auctionId, newEndTime)` to update the Quartz trigger so the `EndAuctionJob` fires at the new end time.
2. **Broadcast via SignalR:** Calls `IAuctionNotificationService.NotifyAuctionExtendedAsync()` to notify connected clients about the extension in real-time.
3. Logs the extension details.

---

## Integration with PlaceBid()

Within `Auction.PlaceBid()`, after the bid is created and price/count updated:

```
1. Create bid, mark as Winning
2. UpdatePriceAndCount()
3. TryAutoExtend()  <-- HERE
4. Raise BidPlacedEvent
5. ProcessAutoBids() for other bidders
```

The auto-extension check happens **before** the `BidPlacedEvent` is raised, ensuring the event contains the correct state. Auto-bids from other bidders (step 5) can themselves trigger additional extensions if they place bids within the threshold window.

---

## Sealed Auctions

Sealed auctions (`AuctionType.Sealed`) have `autoExtend` forced to `false`:

- `UpdateAuctionCommand` handler: If `AuctionType == Sealed && requestedAutoExtend == true` -> returns error `Auction.SealedAutoExtendNotSupported`.
- `SetAuctionTimingCommand` handler: Same check. If sealed and `AutoExtend == true` -> error.
- Since `Info.AutoExtend` is `false`, `TryAutoExtend()` will always no-op (first condition fails).

---

## Example Timeline

Given: ExtensionThreshold = 5 min, ExtensionMinutes = 5, MaxExtensions = 10

| Time | Event | EndTime | ExtensionCount |
|------|-------|---------|----------------|
| T+0:00 | Auction starts, EndTime = T+1:00 | T+1:00 | 0 |
| T+0:54 | Bid placed (6 min before end) | T+1:00 | 0 (not within threshold) |
| T+0:56 | Bid placed (4 min before end) | T+1:05 | 1 (extended!) |
| T+1:02 | Bid placed (3 min before new end) | T+1:10 | 2 (extended!) |
| ... | ... | ... | ... |
| After 10 extensions | Bid placed near end | unchanged | 10 (max reached, no more extensions) |

---

## Event: AuctionExtendedEvent

```
AuctionExtendedEvent
  AuctionId       : string
  TriggerByBidId  : string
  PreviousEndTime : DateTime
  NewEndTime      : DateTime
  ExtensionMinutes: int
  ExtensionCount  : int
  OccurredAt      : DateTime
```
