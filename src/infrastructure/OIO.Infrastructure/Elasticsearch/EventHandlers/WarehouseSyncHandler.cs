using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems.Events;

namespace OIO.Infrastructure.Elasticsearch.EventHandlers;

public class WarehouseSyncHandler(IElasticsearchSyncService syncService) : 
    INotificationHandler<WarehouseItemCreatedEvent>,
    INotificationHandler<WarehouseItemStoredEvent>,
    INotificationHandler<WarehouseItemReservedEvent>,
    INotificationHandler<WarehouseItemDispatchedEvent>
{
    public async Task Handle(WarehouseItemCreatedEvent notification, CancellationToken cancellationToken)
    {
        await syncService.SyncWarehouseItemAsync(Guid.Parse(notification.WarehouseItemId), cancellationToken);
    }

    public async Task Handle(WarehouseItemStoredEvent notification, CancellationToken cancellationToken)
    {
        await syncService.SyncWarehouseItemAsync(Guid.Parse(notification.WarehouseItemId), cancellationToken);
    }

    public async Task Handle(WarehouseItemReservedEvent notification, CancellationToken cancellationToken)
    {
        await syncService.SyncWarehouseItemAsync(Guid.Parse(notification.WarehouseItemId), cancellationToken);
    }

    public async Task Handle(WarehouseItemDispatchedEvent notification, CancellationToken cancellationToken)
    {
        await syncService.SyncWarehouseItemAsync(Guid.Parse(notification.WarehouseItemId), cancellationToken);
    }
}
