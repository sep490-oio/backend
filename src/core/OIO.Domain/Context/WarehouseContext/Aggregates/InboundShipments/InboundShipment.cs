using CSharpFunctionalExtensions;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments.Events;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using e = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;

/// <summary>
/// Represents the shipment of an item from a seller to the warehouse.
///
/// Lifecycle:
///   AwaitingPickup → InTransit → Arrived → Inspected → Completed
///                                                    ↘ Cancelled | Failed (from any pre-completed state)
/// </summary>
public sealed class InboundShipment : AggregateRoot<InboundShipmentId>
{
    private readonly List<ShipmentTrackingEvent> _trackingEvents = [];

    private InboundShipment() { }

    private InboundShipment(
        InboundShipmentId id,
        Guid itemId,
        UserId sellerId,
        ShippingProviderCode providerCode,
        string clientOrderCode,
        string senderName,
        string senderPhone,
        string senderAddress,
        string senderWard,
        string senderDistrict,
        string senderProvince,
        CarrierAddressData? senderCarrierAddressData,
        PackageDimensions dimensions,
        decimal shippingFee,
        decimal insuranceValue,
        ShipmentExtraData extraData,
        string? notes,
        DateTime? expectedArrivalAt,
        DateTime now)
    {
        Id                       = id;
        ItemId                   = itemId;
        SellerId                 = sellerId;
        ProviderCode             = providerCode;
        ClientOrderCode          = clientOrderCode;
        SenderName               = senderName;
        SenderPhone              = senderPhone;
        SenderAddress            = senderAddress;
        SenderWard               = senderWard;
        SenderDistrict           = senderDistrict;
        SenderProvince           = senderProvince;
        SenderCarrierAddressData = senderCarrierAddressData;
        Dimensions               = dimensions;
        ShippingFee              = shippingFee;
        InsuranceValue           = insuranceValue;
        ExtraData                = extraData;
        Notes                    = notes;
        Status                   = InboundShipmentStatus.AwaitingPickup;
        ExpectedArrivalAt        = expectedArrivalAt;
        CreatedAt                = now;
    }

    public Guid ItemId { get; private set; }
    public UserId SellerId { get; private set; }
    public ShippingProviderCode ProviderCode { get; private set; }

    /// <summary>Our internal reference sent to the carrier on order creation.</summary>
    public string ClientOrderCode { get; private set; }

    /// <summary>
    /// Tracking number returned by carrier after booking.
    /// GHN: order_code. GHTK: label. Null until booked.
    /// </summary>
    public string? CarrierTrackingNumber { get; private set; }

    public string SenderName { get; private set; }
    public string SenderPhone { get; private set; }
    public string SenderAddress { get; private set; }
    public string SenderWard { get; private set; }
    public string SenderDistrict { get; private set; }
    public string SenderProvince { get; private set; }

    /// <summary>GHN: { "district_id": 1442, "ward_code": "21012" }. Null for GHTK.</summary>
    public CarrierAddressData? SenderCarrierAddressData { get; private set; }

    public PackageDimensions Dimensions { get; private set; }
    public decimal ShippingFee { get; private set; }
    public decimal InsuranceValue { get; private set; }
    public ShipmentExtraData ExtraData { get; private set; }
    public InboundShipmentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? ExpectedArrivalAt { get; private set; }
    public DateTime? ArrivedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    public IReadOnlyList<ShipmentTrackingEvent> TrackingEvents => _trackingEvents;

    public static InboundShipment Create(
        Guid itemId,
        UserId sellerId,
        ShippingProviderCode providerCode,
        string clientOrderCode,
        string senderName,
        string senderPhone,
        string senderAddress,
        string senderWard,
        string senderDistrict,
        string senderProvince,
        PackageDimensions dimensions,
        DateTime now,
        CarrierAddressData? senderCarrierAddressData = null,
        decimal shippingFee = 0,
        decimal insuranceValue = 0,
        ShipmentExtraData? extraData = null,
        string? notes = null,
        DateTime? expectedArrivalAt = null)
    {
        var shipment = new InboundShipment(
            InboundShipmentId.From(Guid.CreateVersion7()),
            itemId,
            sellerId,
            providerCode,
            clientOrderCode,
            senderName,
            senderPhone,
            senderAddress,
            senderWard,
            senderDistrict,
            senderProvince,
            senderCarrierAddressData,
            dimensions,
            shippingFee,
            insuranceValue,
            extraData ?? ShipmentExtraData.Empty,
            notes,
            expectedArrivalAt,
            now);

        shipment.RaiseDomainEvent(new InboundShipmentCreatedEvent(
            shipment.Id.ToString(),
            itemId.ToString(),
            sellerId.ToString(),
            providerCode.Id,
            clientOrderCode,
            now));

        return shipment;
    }

    /// <summary>
    /// Called after carrier API confirms booking and returns a tracking number.
    /// </summary>
    public UnitResult<e> RecordBooked(string carrierTrackingNumber, DateTime now)
    {
        if (CarrierTrackingNumber is not null)
            return WarehouseErrors.InboundShipment.AlreadyBooked;

        CarrierTrackingNumber = carrierTrackingNumber;
        ModifiedAt = now;

        RaiseDomainEvent(new InboundShipmentBookedEvent(
            Id.ToString(),
            ProviderCode.Id,
            ClientOrderCode,
            carrierTrackingNumber,
            now));

        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Called when carrier webhook reports the item has arrived at the warehouse.
    /// </summary>
    public UnitResult<e> RecordArrived(DateTime now)
    {
        if (Status == InboundShipmentStatus.Arrived)
            return WarehouseErrors.InboundShipment.AlreadyArrived;

        Status     = InboundShipmentStatus.Arrived;
        ArrivedAt  = now;
        ModifiedAt = now;

        RaiseDomainEvent(new InboundShipmentArrivedEvent(
            Id.ToString(),
            ProviderCode.Id,
            CarrierTrackingNumber ?? ClientOrderCode,
            now));

        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Called by warehouse staff after physically inspecting the item.
    /// Triggers WarehouseItem creation downstream.
    /// </summary>
    public UnitResult<e> RecordInspected(UserId inspectedBy, DateTime now)
    {
        if (Status != InboundShipmentStatus.Arrived)
            return WarehouseErrors.InboundShipment.CannotInspect;

        Status     = InboundShipmentStatus.Inspected;
        ModifiedAt = now;

        RaiseDomainEvent(new InboundShipmentInspectedEvent(
            Id.ToString(),
            ItemId.ToString(),
            string.Empty, // condition set on WarehouseItem, not here
            inspectedBy.ToString(),
            now));

        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Called after WarehouseItem has been created and stored.
    /// Final state — no further transitions allowed.
    /// </summary>
    public UnitResult<e> Complete(DateTime now)
    {
        if (Status != InboundShipmentStatus.Inspected)
            return WarehouseErrors.InboundShipment.CannotComplete;

        Status     = InboundShipmentStatus.Completed;
        ModifiedAt = now;

        RaiseDomainEvent(new InboundShipmentCompletedEvent(
            Id.ToString(),
            ItemId.ToString(),
            now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> Cancel(string reason, DateTime now)
    {
        if (Status is { Id: "completed" or "cancelled" or "failed" })
            return WarehouseErrors.InboundShipment.CannotCancel;

        Status     = InboundShipmentStatus.Cancelled;
        ModifiedAt = now;

        RaiseDomainEvent(new InboundShipmentCancelledEvent(
            Id.ToString(), reason, now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> RecordFailed(string reason, DateTime now)
    {
        if (Status is { Id: "completed" or "cancelled" or "failed" })
            return WarehouseErrors.InboundShipment.CannotCancel;

        Status     = InboundShipmentStatus.Failed;
        ModifiedAt = now;

        RaiseDomainEvent(new InboundShipmentFailedEvent(
            Id.ToString(), reason, now));

        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Records a carrier webhook tracking event. Called by the webhook handler.
    /// Also advances shipment status based on normalized status.
    /// </summary>
    public ShipmentTrackingEvent RecordTrackingEvent(
        ShippingProviderCode providerCode,
        string carrierStatusRaw,
        string? carrierStatusDesc,
        NormalizedTrackingStatus normalizedStatus,
        string? location,
        string? reasonCode,
        string? reasonDescription,
        DateTime eventTime,
        WebhookRawPayload rawPayload,
        DateTime now)
    {
        var trackingEvent = new ShipmentTrackingEvent(
            ShipmentTrackingEventId.From(Guid.CreateVersion7()),
            "inbound",
            Id.Value,
            providerCode,
            carrierStatusRaw,
            carrierStatusDesc,
            normalizedStatus,
            location,
            reasonCode,
            reasonDescription,
            eventTime,
            rawPayload,
            now);

        _trackingEvents.Add(trackingEvent);

        // Auto-advance status based on normalized event
        // Auto-advance status based on normalized event
        switch (normalizedStatus.Id)
        {
            case "picked_up" or "in_transit" or "delayed":
                if (Status == InboundShipmentStatus.AwaitingPickup ||
                    Status == InboundShipmentStatus.InTransit)
                {
                    Status     = InboundShipmentStatus.InTransit;
                    ModifiedAt = now;
                }
                break;

            case "delivered":
                if (Status != InboundShipmentStatus.Arrived &&
                    Status != InboundShipmentStatus.Inspected &&
                    Status != InboundShipmentStatus.Completed)
                {
                    Status     = InboundShipmentStatus.Arrived;
                    ArrivedAt  = now;
                    ModifiedAt = now;
                }
                break;

            case "cancelled":
                if (Status is not { Id: "completed" or "cancelled" or "failed" })
                {
                    Status     = InboundShipmentStatus.Cancelled;
                    ModifiedAt = now;
                }
                break;

            case "failed" or "returning" or "returned":
                if (Status is not { Id: "completed" or "cancelled" or "failed" })
                {
                    Status     = InboundShipmentStatus.Failed;
                    ModifiedAt = now;
                }
                break;
        }

        RaiseDomainEvent(new InboundTrackingEventRecordedEvent(
            Id.ToString(),
            normalizedStatus.Id,
            providerCode.Id,
            carrierStatusRaw,
            now));

        return trackingEvent;
    }
}