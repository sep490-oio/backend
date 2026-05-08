using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.CatalogContext.Aggregates.Items.Events;

namespace OIO.Application.Context.SearchContext.EventHandlers;

/// <summary>
/// Handles domain events to automatically synchronize data with Elasticsearch.
/// </summary>
public sealed class SearchSyncEventHandlers(IElasticsearchSyncService syncService) :
    INotificationHandler<AuctionCreatedEvent>,
    INotificationHandler<AuctionScheduledEvent>,
    INotificationHandler<AuctionStartedEvent>,
    INotificationHandler<AuctionCancelledEvent>,
    INotificationHandler<AuctionEndedEvent>,
    INotificationHandler<AuctionSoldEvent>,
    INotificationHandler<AuctionFailedEvent>,
    INotificationHandler<AuctionTerminatedEvent>,
    INotificationHandler<BidPlacedEvent>,
    INotificationHandler<AuctionExtendedEvent>,
    INotificationHandler<AuctionWatcherAddedEvent>,
    INotificationHandler<AuctionFeatureToggledEvent>,
    INotificationHandler<AuctionBuyNowReservedEvent>,
    INotificationHandler<AuctionBuyNowReservationReleasedEvent>,
    INotificationHandler<AuctionBuyNowCompensationExtendedEvent>,
    INotificationHandler<BuyNowExecutedEvent>,
    INotificationHandler<AuctionRelistedEvent>,
    INotificationHandler<ItemCreatedEvent>,
    INotificationHandler<ItemStatusChangedEvent>,
    INotificationHandler<MediaRemovedFromItemEvent>,
    INotificationHandler<ItemUpdatedEvent>,
    INotificationHandler<MediaAddedToItemEvent>,
    INotificationHandler<ItemMediaPrimarySetEvent>,
    INotificationHandler<ItemMediaReorderedEvent>
{
    // ─── Auction Events ──────────────────────────────────────────────────
    
    public Task Handle(AuctionCreatedEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(AuctionScheduledEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(AuctionStartedEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(AuctionCancelledEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(AuctionEndedEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(AuctionSoldEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(AuctionFailedEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(AuctionTerminatedEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(BidPlacedEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(AuctionExtendedEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(AuctionWatcherAddedEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(AuctionFeatureToggledEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(AuctionBuyNowReservedEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(AuctionBuyNowReservationReleasedEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(AuctionBuyNowCompensationExtendedEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(BuyNowExecutedEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), ct);

    public Task Handle(AuctionRelistedEvent notification, CancellationToken ct) =>
        syncService.SyncAuctionAsync(Guid.Parse(notification.NewAuctionId), ct);

    // ─── Item Events ─────────────────────────────────────────────────────

    public Task Handle(ItemCreatedEvent notification, CancellationToken ct) =>
        syncService.SyncItemAsync(Guid.Parse(notification.ItemId), ct);

    public Task Handle(ItemStatusChangedEvent notification, CancellationToken ct) =>
        syncService.SyncItemAsync(Guid.Parse(notification.ItemId), ct);

    public Task Handle(MediaRemovedFromItemEvent notification, CancellationToken ct) =>
        syncService.SyncItemAsync(Guid.Parse(notification.ItemId), ct);

    public Task Handle(ItemUpdatedEvent notification, CancellationToken ct) =>
        syncService.SyncItemAsync(Guid.Parse(notification.ItemId), ct);

    public Task Handle(MediaAddedToItemEvent notification, CancellationToken ct) =>
        syncService.SyncItemAsync(Guid.Parse(notification.ItemId), ct);

    public Task Handle(ItemMediaPrimarySetEvent notification, CancellationToken ct) =>
        syncService.SyncItemAsync(Guid.Parse(notification.ItemId), ct);

    public Task Handle(ItemMediaReorderedEvent notification, CancellationToken ct) =>
        syncService.SyncItemAsync(Guid.Parse(notification.ItemId), ct);
}
