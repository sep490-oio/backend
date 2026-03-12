using CSharpFunctionalExtensions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments.Events;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using e = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;

/// <summary>
/// Represents the shipment of an item from the warehouse to the buyer.
/// Created after order is paid. Replaces the old shipments table.
///
/// Recipient address is NOT duplicated here — read from the orders table via OrderId.
/// Only recipient_carrier_address_data is stored for GHN's integer IDs.
///
/// Lifecycle:
///   Pending → Booked → PickedUp → InTransit → Delivered
///                                           ↘ Failed → Returning → Returned
///          ↘ Cancelled (only before PickedUp)
/// </summary>
public sealed class OutboundShipment : AggregateRoot<OutboundShipmentId>
{
    private readonly List<ShipmentTrackingEvent> _trackingEvents = [];

    private OutboundShipment() { }

    private OutboundShipment(
        OutboundShipmentId id,
        OrderId orderId,
        WarehouseItemId warehouseItemId,
        ShippingProviderCode providerCode,
        string clientOrderCode,
        string? shippingMethod,
        CarrierAddressData? recipientCarrierAddressData,
        PackageDimensions dimensions,
        decimal shippingFee,
        decimal insuranceValue,
        decimal codAmount,
        GhnPaymentType? ghnPaymentType,
        GhnHandlingNote? ghnHandlingNote,
        ShipmentExtraData extraData,
        DateTime now)
    {
        Id                           = id;
        OrderId                      = orderId;
        WarehouseItemId              = warehouseItemId;
        ProviderCode                 = providerCode;
        ClientOrderCode              = clientOrderCode;
        ShippingMethod               = shippingMethod;
        RecipientCarrierAddressData  = recipientCarrierAddressData;
        Dimensions                   = dimensions;
        ShippingFee                  = shippingFee;
        InsuranceValue               = insuranceValue;
        CodAmount                    = codAmount;
        GhnPaymentType               = ghnPaymentType;
        GhnHandlingNote              = ghnHandlingNote;
        ExtraData                    = extraData;
        Status                       = OutboundShipmentStatus.Pending;
        CreatedAt                    = now;
    }

    public OrderId OrderId { get; private set; }
    public WarehouseItemId WarehouseItemId { get; private set; }
    public ShippingProviderCode ProviderCode { get; private set; }

    /// <summary>Our internal reference sent to the carrier on booking.</summary>
    public string ClientOrderCode { get; private set; }

    /// <summary>
    /// Tracking number returned by carrier after booking.
    /// GHN: order_code. GHTK: label. Null until booked.
    /// </summary>
    public string? CarrierTrackingNumber { get; private set; }

    /// <summary>Printable label URL returned by carrier after booking.</summary>
    public string? ShippingLabelUrl { get; private set; }

    public string? ShippingMethod { get; private set; }

    /// <summary>
    /// GHN: { "district_id": 1442, "ward_code": "21012" }.
    /// Null for GHTK. Recipient text address is read from orders table via OrderId.
    /// </summary>
    public CarrierAddressData? RecipientCarrierAddressData { get; private set; }

    public PackageDimensions Dimensions { get; private set; }
    public decimal ShippingFee { get; private set; }
    public decimal InsuranceValue { get; private set; }
    public decimal CodAmount { get; private set; }

    /// <summary>GHN only: who pays the shipping fee. Null for GHTK.</summary>
    public GhnPaymentType? GhnPaymentType { get; private set; }

    /// <summary>GHN only: buyer inspection instruction (required_note). Null for GHTK.</summary>
    public GhnHandlingNote? GhnHandlingNote { get; private set; }

    public ShipmentExtraData ExtraData { get; private set; }
    public OutboundShipmentStatus Status { get; private set; }

    public UserId? PackedBy { get; private set; }
    public DateTime? PackedAt { get; private set; }
    public DateTime? DispatchedAt { get; private set; }
    public DateTime? EstimatedDeliveryAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    public Order Order { get; private set; }
    public IReadOnlyCollection<ShipmentTrackingEvent> TrackingEvents => _trackingEvents.AsReadOnly();

    public static OutboundShipment Create(
        OrderId orderId,
        WarehouseItemId warehouseItemId,
        ShippingProviderCode providerCode,
        string clientOrderCode,
        PackageDimensions dimensions,
        DateTime now,
        string? shippingMethod = null,
        CarrierAddressData? recipientCarrierAddressData = null,
        decimal shippingFee = 0,
        decimal insuranceValue = 0,
        decimal codAmount = 0,
        GhnPaymentType? ghnPaymentType = null,
        GhnHandlingNote? ghnHandlingNote = null,
        ShipmentExtraData? extraData = null)
    {
        var shipment = new OutboundShipment(
            OutboundShipmentId.From(Guid.CreateVersion7()),
            orderId,
            warehouseItemId,
            providerCode,
            clientOrderCode,
            shippingMethod,
            recipientCarrierAddressData,
            dimensions,
            shippingFee,
            insuranceValue,
            codAmount,
            ghnPaymentType,
            ghnHandlingNote,
            extraData ?? ShipmentExtraData.Empty,
            now);

        shipment.RaiseDomainEvent(new OutboundShipmentCreatedEvent(
            shipment.Id.ToString(),
            orderId.ToString(),
            warehouseItemId.ToString(),
            providerCode.Id,
            clientOrderCode,
            now));

        return shipment;
    }

    /// <summary>
    /// Record warehouse staff packing the item before carrier pickup.
    /// </summary>
    public UnitResult<e> RecordPacked(UserId packedBy, DateTime now)
    {
        PackedBy   = packedBy;
        PackedAt   = now;
        ModifiedAt = now;
        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Called after carrier API confirms booking and returns tracking number + label.
    /// </summary>
    public UnitResult<e> RecordBooked(
        string carrierTrackingNumber,
        DateTime now,
        string? shippingLabelUrl = null,
        DateTime? estimatedDeliveryAt = null)
    {
        if (CarrierTrackingNumber is not null)
            return WarehouseErrors.OutboundShipment.AlreadyBooked;

        CarrierTrackingNumber = carrierTrackingNumber;
        ShippingLabelUrl      = shippingLabelUrl;
        EstimatedDeliveryAt   = estimatedDeliveryAt;
        Status                = OutboundShipmentStatus.Booked;
        ModifiedAt            = now;

        RaiseDomainEvent(new OutboundShipmentBookedEvent(
            Id.ToString(),
            ProviderCode.Id,
            ClientOrderCode,
            carrierTrackingNumber,
            shippingLabelUrl,
            now));

        return UnitResult.Success<e>();
    }

    /// <summary>Called when carrier picks up the package from warehouse.</summary>
    public UnitResult<e> RecordPickedUp(DateTime now)
    {
        Status        = OutboundShipmentStatus.PickedUp;
        DispatchedAt  = now;
        ModifiedAt    = now;

        RaiseDomainEvent(new OutboundShipmentPickedUpEvent(
            Id.ToString(),
            ProviderCode.Id,
            CarrierTrackingNumber ?? ClientOrderCode,
            now));

        return UnitResult.Success<e>();
    }

    /// <summary>Called when carrier webhook reports delivery confirmed.</summary>
    public UnitResult<e> RecordDelivered(DateTime deliveredAt, DateTime now)
    {
        if (Status != OutboundShipmentStatus.InTransit &&
            Status != OutboundShipmentStatus.PickedUp)
            return WarehouseErrors.OutboundShipment.CannotMarkDelivered;

        Status      = OutboundShipmentStatus.Delivered;
        DeliveredAt = deliveredAt;
        ModifiedAt  = now;

        RaiseDomainEvent(new OutboundShipmentDeliveredEvent(
            Id.ToString(),
            OrderId.ToString(),
            ProviderCode.Id,
            deliveredAt,
            now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> RecordFailed(string reason, DateTime now)
    {
        Status     = OutboundShipmentStatus.Failed;
        ModifiedAt = now;

        RaiseDomainEvent(new OutboundShipmentFailedEvent(
            Id.ToString(), OrderId.ToString(), reason, now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> RecordReturning(string reason, DateTime now)
    {
        Status     = OutboundShipmentStatus.Returning;
        ModifiedAt = now;

        RaiseDomainEvent(new OutboundShipmentReturningEvent(
            Id.ToString(), OrderId.ToString(), reason, now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> RecordReturned(DateTime now)
    {
        Status     = OutboundShipmentStatus.Returned;
        ModifiedAt = now;

        RaiseDomainEvent(new OutboundShipmentReturnedEvent(
            Id.ToString(), OrderId.ToString(), now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> Cancel(string reason, DateTime now)
    {
        if (Status == OutboundShipmentStatus.PickedUp ||
            Status == OutboundShipmentStatus.InTransit ||
            Status == OutboundShipmentStatus.Delivered)
            return WarehouseErrors.OutboundShipment.CannotCancel;

        Status     = OutboundShipmentStatus.Cancelled;
        ModifiedAt = now;

        RaiseDomainEvent(new OutboundShipmentCancelledEvent(
            Id.ToString(), OrderId.ToString(), reason, now));

        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Records a carrier webhook tracking event.
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
            "outbound",
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

        // Auto-advance status from tracking event
        var newStatus = normalizedStatus switch
        {
            { Id: "picked_up" }  => OutboundShipmentStatus.PickedUp,
            { Id: "in_transit" } => OutboundShipmentStatus.InTransit,
            { Id: "delivered" }  => OutboundShipmentStatus.Delivered,
            { Id: "failed" }     => OutboundShipmentStatus.Failed,
            { Id: "returning" }  => OutboundShipmentStatus.Returning,
            { Id: "returned" }   => OutboundShipmentStatus.Returned,
            _                    => Status
        };

        if (newStatus != Status)
        {
            Status     = newStatus;
            ModifiedAt = now;

            if (newStatus == OutboundShipmentStatus.PickedUp)
                DispatchedAt = eventTime;

            if (newStatus == OutboundShipmentStatus.Delivered)
                DeliveredAt = eventTime;
        }

        RaiseDomainEvent(new OutboundTrackingEventRecordedEvent(
            Id.ToString(),
            normalizedStatus.Id,
            providerCode.Id,
            carrierStatusRaw,
            now));

        return trackingEvent;
    }
}