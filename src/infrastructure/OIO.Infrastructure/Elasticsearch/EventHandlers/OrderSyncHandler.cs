using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Domain.Context.OrderContext.Aggregates.Orders.Events;

namespace OIO.Infrastructure.Elasticsearch.EventHandlers;

public class OrderSyncHandler(IElasticsearchSyncService syncService) : 
    INotificationHandler<OrderCreatedEvent>,
    INotificationHandler<OrderCompletedEvent>,
    INotificationHandler<OrderCancelledEvent>
{
    public async Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        await syncService.SyncOrderAsync(Guid.Parse(notification.OrderId), cancellationToken);
    }

    public async Task Handle(OrderCompletedEvent notification, CancellationToken cancellationToken)
    {
        await syncService.SyncOrderAsync(Guid.Parse(notification.OrderId), cancellationToken);
    }

    public async Task Handle(OrderCancelledEvent notification, CancellationToken cancellationToken)
    {
        await syncService.SyncOrderAsync(Guid.Parse(notification.OrderId), cancellationToken);
    }
}
