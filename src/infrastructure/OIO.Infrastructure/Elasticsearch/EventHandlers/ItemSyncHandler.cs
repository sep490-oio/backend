using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Domain.Context.CatalogContext.Aggregates.Items.Events;

namespace OIO.Infrastructure.Elasticsearch.EventHandlers;

public class ItemSyncHandler : 
    INotificationHandler<ItemCreatedEvent>,
    INotificationHandler<ItemStatusChangedEvent>
{
    private readonly IElasticsearchSyncService _syncService;

    public ItemSyncHandler(IElasticsearchSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task Handle(ItemCreatedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncItemAsync(Guid.Parse(notification.ItemId), cancellationToken);
    }

    public async Task Handle(ItemStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncItemAsync(Guid.Parse(notification.ItemId), cancellationToken);
    }
}
