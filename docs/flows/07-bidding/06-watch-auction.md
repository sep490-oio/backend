# Watch Auction

## Overview

Users can **watch** an auction to receive real-time notifications about bid activity and
auction completion. The watch feature supports per-watcher notification preferences
(`notifyOnBid`, `notifyOnEnd`) and is exposed via both REST endpoints and a SignalR hub
method.

---

## Endpoints

### REST

| Method | Path | Auth | Success | Errors |
|--------|------|------|---------|--------|
| `POST` | `/api/auctions/{auctionId}/watch` | `Auctions.Watch` permission | `204 No Content` | `409 Conflict` |
| `DELETE` | `/api/auctions/{auctionId}/watch` | `Auctions.Unwatch` permission | `204 No Content` | -- |

### SignalR

| Method | Signature | Returns |
|--------|-----------|---------|
| `WatchAuction` | `WatchAuction(auctionId: Guid, notifyOnBid: bool = true, notifyOnEnd: bool = true)` | `void` (errors sent via `Clients.Caller.Error()`) |

---

## Watch Request

### REST (POST)

```csharp
public sealed record Request(
    [Required] bool NotifyOnBid = true,
    [Required] bool NotifyOnEnd = true);
```

Both fields default to `true`. The request body is optional -- if omitted, the command uses
the defaults.

### SignalR

Parameters are passed as hub method arguments with defaults:

```csharp
public async Task WatchAuction(
    Guid auctionId,
    bool notifyOnBid = true,
    bool notifyOnEnd = true)
```

---

## Command: `WatchAuctionCommand`

```csharp
public sealed record WatchAuctionCommand(
    Guid AuctionId,
    bool NotifyOnBid = true,
    bool NotifyOnEnd = true) : ICommand, IHasValidate;
```

**Validation:** `AuctionId` must be a non-empty GUID.

**Handler logic:**

1. Load auction with `Watchers` and `Item` included.
2. Return `Auction.NotFound` if auction does not exist.
3. Call `auction.AddWatcher(currentUserId, nowUtc, notifyOnBid, notifyOnEnd)`.
4. Save changes via `IUnitOfWork`.

---

## Command: `UnwatchAuctionCommand`

```csharp
public sealed record UnwatchAuctionCommand(Guid AuctionId) : ICommand, IHasValidate;
```

**Handler logic:**

1. Load auction with `Watchers` included.
2. Return `Auction.NotFound` if auction does not exist.
3. Call `auction.RemoveWatcher(currentUserId, nowUtc)`.
4. Save changes. `RemoveWatcher` is idempotent -- silently returns if no watcher found.

---

## Domain Logic

### `Auction.AddWatcher`

```csharp
public Result<AuctionWatcher, Error> AddWatcher(
    UserId userId,
    DateTime nowUtc,
    bool notifyOnBid = true,
    bool notifyOnEnd = true)
```

**Guards:**

| Check | Error |
|-------|-------|
| User is already watching | `Watcher.AlreadyWatching` (409 Conflict) |
| User is the auction seller | `Watcher.CannotWatchOwnAuction` (403 Forbidden) |

**On success:**

1. Creates an `AuctionWatcher` entity with the given notification preferences.
2. Increments `Auction.WatchCount`.
3. Raises `AuctionWatcherAddedEvent(AuctionId, UserId, OccurredAt)`.

### `Auction.RemoveWatcher`

```csharp
public void RemoveWatcher(UserId userId, DateTime nowUtc)
```

Removes the watcher from the internal `_watchers` list and decrements `WatchCount`
(clamped to 0). Idempotent -- no error if watcher not found.

---

## AuctionWatcher Entity

```csharp
public sealed class AuctionWatcher : BaseEntity<AuctionWatcherId>, ICreatedAtEntity
{
    public AuctionId AuctionId { get; }
    public UserId UserId { get; }
    public bool NotifyOnBid { get; }
    public bool NotifyOnEnd { get; }
    public DateTime CreatedAt { get; }
}
```

### Notification Preferences

Each watcher stores two boolean preferences:

| Preference | Default | Description |
|------------|---------|-------------|
| `NotifyOnBid` | `true` | Receive notifications when a new bid is placed |
| `NotifyOnEnd` | `true` | Receive notification when the auction ends |

Preferences can be updated via `Auction.UpdateWatcherPreferences(userId, nowUtc, notifyOnBid?, notifyOnEnd?)`.
The `UpdateNotificationSettings` method on the entity applies only provided values, leaving
others unchanged.

---

## Domain Event

### `AuctionWatcherAddedEvent`

```csharp
public sealed record AuctionWatcherAddedEvent(
    string AuctionId,
    string UserId,
    DateTime OccurredAt) : DomainEvent(OccurredAt);
```

The `AuctionWatcherAddedEventHandler` currently logs the event. No further side effects.

---

## Error Codes

| Code | HTTP | Description |
|------|------|-------------|
| `Watcher.AlreadyWatching` | 409 | User is already watching this auction |
| `Watcher.CannotWatchOwnAuction` | 403 | Sellers cannot watch their own auctions |
| `Watcher.NotFound` | 404 | No watcher record found for the given auction and user |
| `Auction.NotFound` | 404 | Auction with the given ID was not found |
