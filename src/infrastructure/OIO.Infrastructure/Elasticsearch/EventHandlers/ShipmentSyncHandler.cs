using MediatR;
using OIO.Application.Abstractions.Search;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments.Events;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments.Events;

namespace OIO.Infrastructure.Elasticsearch.EventHandlers;

public class ShipmentSyncHandler : 
    INotificationHandler<InboundShipmentCreatedEvent>,
    INotificationHandler<InboundShipmentBookedEvent>,
    INotificationHandler<InboundTrackingEventRecordedEvent>,
    INotificationHandler<OutboundShipmentCreatedEvent>,
    INotificationHandler<OutboundShipmentBookedEvent>,
    INotificationHandler<OutboundTrackingEventRecordedEvent>
{
    private readonly IElasticsearchSyncService _syncService;

    public ShipmentSyncHandler(IElasticsearchSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task Handle(InboundShipmentCreatedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncShipmentAsync(Guid.Parse(notification.InboundShipmentId), false, cancellationToken);
    }

    public async Task Handle(InboundShipmentBookedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncShipmentAsync(Guid.Parse(notification.InboundShipmentId), false, cancellationToken);
    }

    public async Task Handle(InboundTrackingEventRecordedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncShipmentAsync(Guid.Parse(notification.InboundShipmentId), false, cancellationToken);
    }

    public async Task Handle(OutboundShipmentCreatedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncShipmentAsync(Guid.Parse(notification.OutboundShipmentId), true, cancellationToken);
    }

    public async Task Handle(OutboundShipmentBookedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncShipmentAsync(Guid.Parse(notification.OutboundShipmentId), true, cancellationToken);
    }

    public async Task Handle(OutboundTrackingEventRecordedEvent notification, CancellationToken cancellationToken)
    {
        await _syncService.SyncShipmentAsync(Guid.Parse(notification.OutboundShipmentId), true, cancellationToken);
    }
}
