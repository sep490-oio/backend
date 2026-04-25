using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

namespace OIO.Infrastructure.Elasticsearch.EventHandlers;

public class AuctionSyncHandler : 
    INotificationHandler<AuctionCreatedEvent>,
    INotificationHandler<AuctionApprovedEvent>,
    INotificationHandler<AuctionStartedEvent>,
    INotificationHandler<AuctionEndedEvent>,
    INotificationHandler<BidPlacedEvent>,
    INotificationHandler<AuctionWatcherAddedEvent>,
    INotificationHandler<AuctionWatcherRemovedEvent>
{
    private readonly IElasticsearchSyncService _syncService;

    public AuctionSyncHandler(IElasticsearchSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task Handle(AuctionCreatedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), cancellationToken);
    }

    public async Task Handle(AuctionApprovedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), cancellationToken);
    }

    public async Task Handle(AuctionStartedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), cancellationToken);
    }

    public async Task Handle(AuctionEndedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), cancellationToken);
    }

    public async Task Handle(BidPlacedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), cancellationToken);
    }

    public async Task Handle(AuctionWatcherAddedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), cancellationToken);
    }

    public async Task Handle(AuctionWatcherRemovedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncAuctionAsync(Guid.Parse(notification.AuctionId), cancellationToken);
    }
}
