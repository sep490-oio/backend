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
/// Created when warehouse staff receive an inbound package.
///
/// Lifecycle:
///   Pending → Received → Stored → Inspected → Reserved → Dispatched
///
/// Receive happens at the package level (warehouse staff). Store assigns the
/// item to a shelf. Inspection happens AFTER the item is on a shelf — the
/// happy path for MarkInspected is the Stored state. Received is also accepted
/// for backward compatibility with legacy data that bypassed the store step.
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

    /// <summary>
    /// Bug #8 fix: link to the auction this physical item is currently committed to.
    /// Null until LinkToAuction is called (typically by an event handler when the
    /// auction is created). Prevents the same physical item from being listed in
    /// multiple concurrent auctions — see LinkToAuction().
    /// </summary>
    public Guid? AuctionId { get; private set; }

    /// <summary>
    /// Bug #8 fix: link to the order produced when AuctionSoldEvent fires.
    /// Null until LinkToOrder is called.
    /// </summary>
    public Guid? OrderId { get; private set; }

    public WarehouseItemStatus Status { get; private set; }
    public DateTime? ReceivedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    /// <summary>
    /// Bug #8 fix: bind this physical item to an auction. Fails if already bound to
    /// a different auction (prevents same item being listed in two auctions
    /// simultaneously — collision was previously only detected at dispatch time).
    /// Idempotent for the same auctionId.
    /// </summary>
    public UnitResult<e> LinkToAuction(Guid auctionId, DateTime now)
    {
        if (AuctionId.HasValue && AuctionId.Value != auctionId)
            return WarehouseErrors.WarehouseItem.AlreadyBoundToAuction(Id, AuctionId.Value);

        if (AuctionId == auctionId)
            return UnitResult.Success<e>();

        AuctionId = auctionId;
        ModifiedAt = now;
        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Bug #8 fix: clear the auction binding (called when auction is Cancelled/Failed/
    /// Terminated and the physical item should become available again).
    /// </summary>
    public UnitResult<e> UnlinkAuction(DateTime now)
    {
        if (AuctionId is null)
            return UnitResult.Success<e>();

        AuctionId = null;
        ModifiedAt = now;
        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Bug #8 fix: bind this item to an order on auction-sold. Idempotent for same orderId.
    /// </summary>
    public UnitResult<e> LinkToOrder(Guid orderId, DateTime now)
    {
        if (OrderId.HasValue && OrderId.Value != orderId)
            return WarehouseErrors.WarehouseItem.AlreadyBoundToOrder(Id, OrderId.Value);

        if (OrderId == orderId)
            return UnitResult.Success<e>();

        OrderId = orderId;
        ModifiedAt = now;
        return UnitResult.Success<e>();
    }

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

    /// <summary>
    /// Mark physical inspection as completed. Inspection details are stored separately.
    /// Happy path: the item is already <see cref="WarehouseItemStatus.Stored"/> on a shelf.
    /// Received is also accepted for backward compatibility with legacy data that bypassed
    /// the store step. StorageLocationId is preserved when the item was already stored.
    /// </summary>
    public UnitResult<e> MarkInspected(DateTime now)
    {
        if (Status != WarehouseItemStatus.Stored && Status != WarehouseItemStatus.Received)
            return WarehouseErrors.WarehouseItem.NotReceived;

        Status     = WarehouseItemStatus.Inspected;
        ModifiedAt = now;

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

    /// <summary>
    /// Reverts the reservation of an item when a shipment is cancelled BEFORE pickup.
    /// Keeps the storage location and resets status to Stored.
    /// </summary>
    public UnitResult<e> UndoReserve(DateTime now)
    {
        if (Status != WarehouseItemStatus.Reserved)
            return UnitResult.Success<e>();

        Status     = WarehouseItemStatus.Stored;
        ModifiedAt = now;

        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Marks the item as returned to the warehouse after a failed delivery.
    /// Status is reset to Received to force a re-storage flow.
    /// StorageLocationId is already null from the previous MarkDispatched call.
    /// </summary>
    public UnitResult<e> MarkReturned(DateTime now)
    {
        if (Status != WarehouseItemStatus.Dispatched)
            return WarehouseErrors.WarehouseItem.NotDispatched;

        Status     = WarehouseItemStatus.Received;
        ModifiedAt = now;

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

    /// <summary>Relocate item within the warehouse.</summary>
    public UnitResult<e> Move(WarehouseStorageLocationId locationId, string locationLabel, DateTime now)
    {
        if (Status != WarehouseItemStatus.Received && Status != WarehouseItemStatus.Inspected && Status != WarehouseItemStatus.Stored)
            return WarehouseErrors.WarehouseItem.NotAvailable;

        StorageLocationId = locationId;
        ModifiedAt        = now;

        RaiseDomainEvent(new WarehouseItemStoredEvent(
            Id.ToString(),
            locationId.ToString(),
            locationLabel,
            now));

        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Explicitly remove the item from its storage location.
    /// Called when the item physically leaves the warehouse (e.g., return shipment dispatched).
    /// </summary>
    public UnitResult<e> ClearStorageLocation(DateTime now)
    {
        if (StorageLocationId is not null)
        {
            StorageLocationId = null;
            ModifiedAt        = now;
        }
        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Transition the item into the warehouse→seller return flow. Called when a
    /// warehouse inspector rejects the item and the platform routes it back to
    /// the seller. Allowed from {Received, Inspected, Stored}.
    /// </summary>
    public UnitResult<e> StartReturnToSeller(DateTime now)
    {
        if (Status != WarehouseItemStatus.Received
            && Status != WarehouseItemStatus.Inspected
            && Status != WarehouseItemStatus.Stored)
            return WarehouseErrors.WarehouseItem.CannotStartReturnToSeller;

        Status     = WarehouseItemStatus.AwaitingSellerReturn;
        ModifiedAt = now;
        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Reverts the item back to the warehouse queue when a return is cancelled (e.g., reinspection requested).
    /// </summary>
    public UnitResult<e> UndoReturnToSeller(DateTime now)
    {
        if (Status != WarehouseItemStatus.AwaitingSellerReturn)
            return WarehouseErrors.WarehouseItem.InvalidState;

        Status     = WarehouseItemStatus.Stored; // Reset to Stored so it shows up in queue
        ModifiedAt = now;
        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Transition an in-flight warehouse→seller return to the "awaiting disposition"
    /// bucket after a delivery failure. The only legal predecessor is
    /// <see cref="WarehouseItemStatus.AwaitingSellerReturn"/> — the shipment aggregate
    /// is already flipped to <c>ReturnedToWarehouse</c> by the staff command.
    /// </summary>
    public UnitResult<e> MarkAwaitingDisposition(DateTime now)
    {
        if (Status != WarehouseItemStatus.AwaitingSellerReturn)
            return WarehouseErrors.WarehouseItem.CannotMarkAwaitingDisposition;

        Status     = WarehouseItemStatus.AwaitingDisposition;
        ModifiedAt = now;
        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Transition the item to ReturnedToSeller when the warehouse-to-seller return shipment
    /// is delivered and confirmed by the seller. This is a terminal state for the physical item
    /// in the warehouse network.
    /// </summary>
    public UnitResult<e> MarkReturnedToSeller(DateTime now)
    {
        if (Status != WarehouseItemStatus.AwaitingSellerReturn)
            return WarehouseErrors.WarehouseItem.InvalidState;

        Status     = WarehouseItemStatus.ReturnedToSeller;
        ModifiedAt = now;
        return UnitResult.Success<e>();
    }

    /// <summary>Adjust item status manually (e.g., Damaged, Lost).</summary>
    public UnitResult<e> AdjustStatus(WarehouseItemStatus newStatus, DateTime now)
    {
        Status     = newStatus;
        ModifiedAt = now;

        // If no longer in regular storage (Lost, Damaged, Dispatched, etc.), free the location
        if (newStatus == WarehouseItemStatus.Lost || 
            newStatus == WarehouseItemStatus.Damaged ||
            newStatus == WarehouseItemStatus.Dispatched)
        {
            StorageLocationId = null;
        }

        return UnitResult.Success<e>();
    }
}
