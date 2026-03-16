using CSharpFunctionalExtensions;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems.Events;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using e = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;

/// <summary>
/// Represents a physical item stored inside the warehouse.
/// Created when an InboundShipment reaches Inspected status.
///
/// Lifecycle:
///   Pending → Received → Inspected → Stored → Reserved → Dispatched
/// </summary>
public sealed class WarehouseItem : AggregateRoot<WarehouseItemId>
{
    private WarehouseItem() { }

    private WarehouseItem(
        WarehouseItemId id,
        Guid itemId,
        InboundShipmentId inboundShipmentId,
        DateTime now)
    {
        Id                  = id;
        ItemId              = itemId;
        InboundShipmentId   = inboundShipmentId;
        Status              = WarehouseItemStatus.Pending;
        CreatedAt           = now;
    }

    public Guid ItemId { get; private set; }
    public InboundShipmentId InboundShipmentId { get; private set; }

    /// <summary>Storage location — null until item is placed on a shelf.</summary>
    public WarehouseStorageLocationId? StorageLocationId { get; private set; }

    public WarehouseItemStatus Status { get; private set; }
    public DateTime? ReceivedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    public static WarehouseItem Create(
        Guid itemId,
        InboundShipmentId inboundShipmentId,
        DateTime now)
    {
        var item = new WarehouseItem(
            WarehouseItemId.From(Guid.CreateVersion7()),
            itemId,
            inboundShipmentId,
            now);

        item.RaiseDomainEvent(new WarehouseItemCreatedEvent(
            item.Id.ToString(),
            itemId.ToString(),
            inboundShipmentId.ToString(),
            now));

        return item;
    }

    /// <summary>Mark item as physically received at warehouse (before full inspection).</summary>
    public UnitResult<e> MarkReceived(DateTime now)
    {
        Status     = WarehouseItemStatus.Received;
        ReceivedAt = now;
        ModifiedAt = now;
        return UnitResult.Success<e>();
    }

    /// <summary>Mark physical inspection as completed. Inspection details are stored separately.</summary>
    public UnitResult<e> MarkInspected(DateTime now)
    {
        if (Status != WarehouseItemStatus.Received)
            return WarehouseErrors.WarehouseItem.NotInspected;

        Status             = WarehouseItemStatus.Inspected;
        ModifiedAt         = now;

        return UnitResult.Success<e>();
    }

    /// <summary>Assign a physical storage location to this item.</summary>
    public UnitResult<e> Store(WarehouseStorageLocationId locationId, string locationLabel, DateTime now)
    {
        if (Status != WarehouseItemStatus.Inspected)
            return WarehouseErrors.WarehouseItem.NotInspected;

        if (StorageLocationId is not null)
            return WarehouseErrors.WarehouseItem.AlreadyStored;

        StorageLocationId = locationId;
        Status            = WarehouseItemStatus.Stored;
        ModifiedAt        = now;

        RaiseDomainEvent(new WarehouseItemStoredEvent(
            Id.ToString(),
            locationId.ToString(),
            locationLabel,
            now));

        return UnitResult.Success<e>();
    }

    /// <summary>Reserve item for an outbound shipment after order is paid.</summary>
    public UnitResult<e> Reserve(OutboundShipmentId outboundShipmentId, DateTime now)
    {
        if (Status != WarehouseItemStatus.Stored)
            return WarehouseErrors.WarehouseItem.NotAvailable;

        Status     = WarehouseItemStatus.Reserved;
        ModifiedAt = now;

        RaiseDomainEvent(new WarehouseItemReservedEvent(
            Id.ToString(),
            outboundShipmentId.ToString(),
            now));

        return UnitResult.Success<e>();
    }

    /// <summary>Mark item as dispatched — called when outbound shipment is picked up by carrier.</summary>
    public UnitResult<e> MarkDispatched(OutboundShipmentId outboundShipmentId, DateTime now)
    {
        if (Status != WarehouseItemStatus.Reserved)
            return WarehouseErrors.WarehouseItem.NotAvailable;

        StorageLocationId = null; // freed from shelf
        Status            = WarehouseItemStatus.Dispatched;
        ModifiedAt        = now;

        RaiseDomainEvent(new WarehouseItemDispatchedEvent(
            Id.ToString(),
            outboundShipmentId.ToString(),
            now));

        return UnitResult.Success<e>();
    }
}
