using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments.Events;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Application.Context.WarehouseContext.EventHandlers;

/// <summary>
/// Syncs the status and storage location of a WarehouseItem when its linked
/// OutboundShipment status changes (PickedUp or Cancelled).
/// </summary>
internal sealed class WarehouseItemShipmentSyncEventHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    ILogger<WarehouseItemShipmentSyncEventHandler> logger)
    : INotificationHandler<OutboundShipmentPickedUpEvent>,
      INotificationHandler<OutboundShipmentCancelledEvent>,
      INotificationHandler<OutboundShipmentReturnedEvent>
{
    public async Task Handle(OutboundShipmentPickedUpEvent notification, CancellationToken cancellationToken)
    {
        var shipmentId = OutboundShipmentId.From(Guid.Parse(notification.OutboundShipmentId));
        
        var shipment = await dbContext.Set<OutboundShipment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment?.WarehouseItemId == null)
            return;

        var warehouseItem = await dbContext.Set<WarehouseItem>()
            .FirstOrDefaultAsync(wi => wi.Id == shipment.WarehouseItemId, cancellationToken);

        if (warehouseItem == null)
        {
            logger.LogWarning("WarehouseItem {WarehouseItemId} not found for shipment {ShipmentId}.", 
                shipment.WarehouseItemId, shipment.Id);
            return;
        }

        var oldLocationId = warehouseItem.StorageLocationId;

        var result = warehouseItem.MarkDispatched(shipmentId, notification.OccurredOn);
        if (result.IsFailure)
        {
            logger.LogError("Failed to mark WarehouseItem {WarehouseItemId} as dispatched: {Error}", 
                warehouseItem.Id, result.Error.Message);
            return;
        }

        if (oldLocationId is not null)
        {
            var location = await dbContext.Set<WarehouseStorageLocation>()
                .FirstOrDefaultAsync(l => l.Id == oldLocationId, cancellationToken);
            location?.MarkVacant();
        }

        dbContext.Update(warehouseItem);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("WarehouseItem {WarehouseItemId} marked as dispatched and location freed for shipment {ShipmentId}.", 
            warehouseItem.Id, shipment.Id);
    }

    public async Task Handle(OutboundShipmentCancelledEvent notification, CancellationToken cancellationToken)
    {
        var shipmentId = OutboundShipmentId.From(Guid.Parse(notification.OutboundShipmentId));
        
        var shipment = await dbContext.Set<OutboundShipment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment?.WarehouseItemId == null)
            return;

        var warehouseItem = await dbContext.Set<WarehouseItem>()
            .FirstOrDefaultAsync(wi => wi.Id == shipment.WarehouseItemId, cancellationToken);

        if (warehouseItem == null)
            return;

        var result = warehouseItem.UndoReserve(notification.OccurredOn);
        if (result.IsFailure)
        {
            logger.LogError("Failed to undo reservation for WarehouseItem {WarehouseItemId} on shipment {ShipmentId} cancellation: {Error}", 
                warehouseItem.Id, shipment.Id, result.Error.Message);
            return;
        }

        dbContext.Update(warehouseItem);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("WarehouseItem {WarehouseItemId} reservation undone (kept on shelf) due to shipment {ShipmentId} cancellation.", 
            warehouseItem.Id, shipment.Id);
    }

    public async Task Handle(OutboundShipmentReturnedEvent notification, CancellationToken cancellationToken)
    {
        var shipmentId = OutboundShipmentId.From(Guid.Parse(notification.OutboundShipmentId));
        
        var shipment = await dbContext.Set<OutboundShipment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment?.WarehouseItemId == null)
            return;

        var warehouseItem = await dbContext.Set<WarehouseItem>()
            .FirstOrDefaultAsync(wi => wi.Id == shipment.WarehouseItemId, cancellationToken);

        if (warehouseItem == null)
            return;

        var result = warehouseItem.MarkReturned(notification.OccurredOn);
        if (result.IsFailure)
        {
            logger.LogError("Failed to mark WarehouseItem {WarehouseItemId} as returned for shipment {ShipmentId}: {Error}", 
                warehouseItem.Id, shipment.Id, result.Error.Message);
            return;
        }

        dbContext.Update(warehouseItem);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("WarehouseItem {WarehouseItemId} marked as returned (requires re-entry) for shipment {ShipmentId}.", 
            warehouseItem.Id, shipment.Id);
    }
}
