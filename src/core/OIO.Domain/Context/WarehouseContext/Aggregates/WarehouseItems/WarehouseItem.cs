using CSharpFunctionalExtensions;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems.Events;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.Shared.Entities;
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
    private readonly List<WarehouseItemMedia> _media = [];
    public IReadOnlyList<WarehouseItemMedia> Media => _media.AsReadOnly();

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

    /// <summary>Mark item as physically received at warehouse.</summary>
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

    public void AddMedia(
        DateTime nowUtc,
        MediaUpload upload,
        bool isPrimary,
        int maxForType,
        int sortOrder)
    {
        var existingForType = _media.Where(m => m.ResourceType == upload.ResourceType).ToList();
        
        // Remove primary flag from existing item if this one is intended to be primary
        if (isPrimary || existingForType.Count == 0)
        {
            foreach (var existing in existingForType)
            {
                existing.UnsetPrimary();
            }
            isPrimary = true;
        }

        var image = WarehouseItemMedia.Create(nowUtc, Id, upload.ResourceType, isPrimary, sortOrder, upload.StorageRef, upload.Info);
        _media.Add(image);
        
        while (_media.Count(m => m.ResourceType == upload.ResourceType) > maxForType)
        {
            var oldest = _media
                .Where(m => m.ResourceType == upload.ResourceType)
                .OrderBy(m => m.CreatedAt)
                .First();
            _media.Remove(oldest);
        }
        
        this.ReorderMediaImages(upload.ResourceType);
        
        // Ensure there is still 1 primary image
        var mediaForType = _media.Where(m => m.ResourceType == upload.ResourceType).ToList();
        if (mediaForType.Count > 0 && mediaForType.All(m => !m.IsPrimary))
        {
            mediaForType.First().SetAsPrimary();
        }

        ModifiedAt = nowUtc;
    }

    private void ReorderMediaImages(string resourceType)
    {
        var orderedMedia = _media
            .Where(m => m.ResourceType == resourceType)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .ToList();

        for (var i = 0; i < orderedMedia.Count; i++)
        {
            orderedMedia[i].Reorder(i);
        }
    }

    /// <summary>Assign a physical storage location to this item.</summary>
    public UnitResult<e> Store(WarehouseStorageLocationId locationId, string locationLabel, DateTime now)
    {
        if (Status != WarehouseItemStatus.Received && Status != WarehouseItemStatus.Inspected)
            return WarehouseErrors.WarehouseItem.NotReceived;

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
        if (Status != WarehouseItemStatus.Received && Status != WarehouseItemStatus.Inspected && Status != WarehouseItemStatus.Stored)
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
        if (Status != WarehouseItemStatus.Received && Status != WarehouseItemStatus.Inspected && 
            Status != WarehouseItemStatus.Stored && Status != WarehouseItemStatus.Reserved)
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
