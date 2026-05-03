using CSharpFunctionalExtensions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.Shared.Errors;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
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
    private readonly List<OutboundShipmentEvidence> _evidence = [];

    private OutboundShipment() { }

    private OutboundShipment(
        OutboundShipmentId id,
        OrderId orderId,
        WarehouseItemId? warehouseItemId,
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
        OutboundShipmentMode shipmentMode,
        string? externalCarrierName,
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
        ShipmentMode                 = shipmentMode;
        ExternalCarrierName          = externalCarrierName;
        Status                       = OutboundShipmentStatus.Pending;
        CreatedAt                    = now;
    }

    public OrderId OrderId { get; private set; }
    public WarehouseItemId? WarehouseItemId { get; private set; }
    public OutboundShipmentMode ShipmentMode { get; private set; }
    public string? ExternalCarrierName { get; private set; }
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
    public DateTime? BuyerReceivedPackageAt { get; private set; }
    public DateTime? BuyerAcceptedAt { get; private set; }
    public string? BuyerAcknowledgedSource { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    public Order Order { get; private set; }
    public IReadOnlyCollection<ShipmentTrackingEvent> TrackingEvents => _trackingEvents.AsReadOnly();
    public IReadOnlyCollection<OutboundShipmentEvidence> Evidence => _evidence.AsReadOnly();

    public bool HasBuyerReceiptProof => _evidence.Any(e => e.Category == "buyer_receipt_photo");

    /// <summary>
    /// Attaches an evidence photo to this shipment. The <paramref name="upload"/>
    /// must already be confirmed. Caller is responsible for linking + relocating.
    /// </summary>
    public OutboundShipmentEvidence AddEvidence(DateTime now, string category, MediaUpload upload)
    {
        var evidence = new OutboundShipmentEvidence(
            OutboundShipmentEvidenceId.From(Guid.CreateVersion7()),
            Id,
            category,
            upload.Id,
            upload.Info.SecureUrl,
            upload.Info.FileName,
            upload.ResourceType,
            DateTime.SpecifyKind(now, DateTimeKind.Utc));

        _evidence.Add(evidence);
        ModifiedAt = now;
        return evidence;
    }

    /// <summary>
    /// Refreshes the snapshot URL/metadata of the <see cref="OutboundShipmentEvidence"/>
    /// matching <paramref name="mediaUploadId"/>. Called by the media relocation
    /// pipeline after Cloudinary rename so the cached evidence URL no longer
    /// points to the <c>/pending/</c> folder.
    /// </summary>
    public UnitResult<e> RefreshEvidenceSnapshot(
        MediaUploadId mediaUploadId,
        MediaInfo info,
        DateTime nowUtc)
    {
        var evidence = _evidence.FirstOrDefault(e => e.MediaUploadId == mediaUploadId);
        if (evidence is null)
            return MediaErrors.ShipmentEvidenceNotFound;

        evidence.UpdateSnapshot(info);
        ModifiedAt = nowUtc;
        return UnitResult.Success<e>();
    }

    // QR token lifecycle — only populated for ExternalCarrier shipments. Version
    // starts at 0, bumped each time a new QR is issued; revoke timestamps audit.
    public string? QrPayload { get; private set; }
    public string? QrCodeUrl { get; private set; }
    public int QrTokenVersion { get; private set; }
    public DateTime? QrTokenIssuedAt { get; private set; }
    public DateTime? QrTokenRevokedAt { get; private set; }

    /// <summary>
    /// Issues a new QR token for an ExternalCarrier shipment. Bumps
    /// <see cref="QrTokenVersion"/>, stamps <see cref="QrTokenIssuedAt"/>, and
    /// clears any previous revoke.
    /// </summary>
    public UnitResult<e> IssueQr(string qrPayload, string? qrCodeUrl, DateTime now)
    {
        QrPayload        = qrPayload;
        QrCodeUrl        = qrCodeUrl;
        QrTokenVersion  += 1;
        QrTokenIssuedAt  = DateTime.SpecifyKind(now, DateTimeKind.Utc);
        QrTokenRevokedAt = null;
        ModifiedAt       = now;
        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Idempotently revokes the current QR token. No-op if already revoked.
    /// </summary>
    public UnitResult<e> RevokeQr(DateTime now)
    {
        if (QrTokenRevokedAt is not null) return UnitResult.Success<e>();

        QrTokenRevokedAt = DateTime.SpecifyKind(now, DateTimeKind.Utc);
        ModifiedAt       = now;
        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Buyer stamps that they physically received the package. Stamp-only —
    /// does NOT change Status. Idempotent: subsequent calls leave the original
    /// timestamp untouched. Allowed from PickedUp, InTransit, Delivering, Delivered.
    /// </summary>
    public UnitResult<e> AcknowledgeReceivedByBuyer(DateTime now, string source)
    {
        if (BuyerReceivedPackageAt is not null)
            return UnitResult.Success<e>();

        if (Status != OutboundShipmentStatus.PickedUp &&
            Status != OutboundShipmentStatus.InTransit &&
            Status != OutboundShipmentStatus.Delivering &&
            Status != OutboundShipmentStatus.Delivered)
            return e.Validation(
                "status",
                "OutboundShipment.CannotAcknowledgeReceived",
                $"Cannot acknowledge receipt while shipment is in status '{Status.Id}'.");

        BuyerReceivedPackageAt  = DateTime.SpecifyKind(now, DateTimeKind.Utc);
        BuyerAcknowledgedSource = source;
        ModifiedAt              = now;
        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Buyer accepts the package as satisfactory. Stamp-only — does NOT change
    /// Status. Idempotent: subsequent calls leave the original timestamp
    /// untouched. Allowed from PickedUp, InTransit, Delivering, Delivered.
    /// </summary>
    public UnitResult<e> MarkBuyerAccepted(DateTime now)
    {
        if (BuyerAcceptedAt is not null)
            return UnitResult.Success<e>();

        if (Status != OutboundShipmentStatus.PickedUp &&
            Status != OutboundShipmentStatus.InTransit &&
            Status != OutboundShipmentStatus.Delivering &&
            Status != OutboundShipmentStatus.Delivered)
            return e.Validation(
                "status",
                "OutboundShipment.CannotMarkBuyerAccepted",
                $"Cannot mark buyer-accepted while shipment is in status '{Status.Id}'.");

        BuyerAcceptedAt = DateTime.SpecifyKind(now, DateTimeKind.Utc);
        ModifiedAt      = now;
        return UnitResult.Success<e>();
    }

    public static OutboundShipment Create(
        OrderId orderId,
        WarehouseItemId? warehouseItemId,
        ShippingProviderCode providerCode,
        string clientOrderCode,
        PackageDimensions dimensions,
        DateTime now,
        OutboundShipmentMode? shipmentMode = null,
        string? externalCarrierName = null,
        string? shippingMethod = null,
        CarrierAddressData? recipientCarrierAddressData = null,
        decimal shippingFee = 0,
        decimal insuranceValue = 0,
        decimal codAmount = 0,
        GhnPaymentType? ghnPaymentType = null,
        GhnHandlingNote? ghnHandlingNote = null,
        ShipmentExtraData? extraData = null)
    {
        var mode = shipmentMode ?? OutboundShipmentMode.PlatformManaged;

        if (mode == OutboundShipmentMode.SellerSelfShip && warehouseItemId != null)
        {
            // Self-ship items don't go through warehouse items
            // but we might allow linking if needed? For now, keep it separate.
        }

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
            mode,
            externalCarrierName,
            now);

        shipment.RaiseDomainEvent(new OutboundShipmentCreatedEvent(
            shipment.Id.ToString(),
            orderId.ToString(),
            warehouseItemId?.ToString() ?? string.Empty,
            providerCode.Id,
            clientOrderCode,
            now));

        return shipment;
    }

    public UnitResult<e> RecordSellerShipped(string carrierTrackingNumber, DateTime now)
    {
        if (ShipmentMode != OutboundShipmentMode.SellerSelfShip)
            return WarehouseErrors.OutboundShipment.NotSellerSelfShip;

        if (CarrierTrackingNumber is not null)
            return WarehouseErrors.OutboundShipment.AlreadyShipped;

        CarrierTrackingNumber = carrierTrackingNumber;
        Status                = OutboundShipmentStatus.InTransit;
        ModifiedAt            = now;
        DispatchedAt          = now;

        RaiseDomainEvent(new OutboundShipmentPickedUpEvent(
            Id.ToString(),
            OrderId.ToString(),
            ProviderCode.Id,
            carrierTrackingNumber,
            now));

        return UnitResult.Success<e>();
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

    /// <summary>
    /// Mark an external-carrier shipment as dispatched/in-transit immediately
    /// after booking. Used by the external-carrier flow where there's no
    /// integrated carrier webhook to drive status progression.
    /// </summary>
    public UnitResult<e> MarkDispatched(DateTime now)
    {
        Status       = OutboundShipmentStatus.InTransit;
        DispatchedAt = now;
        ModifiedAt   = now;

        RaiseDomainEvent(new OutboundShipmentInTransitEvent(
            Id.ToString(), OrderId.ToString(), ProviderCode.Id, now));

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
            OrderId.ToString(),
            ProviderCode.Id,
            CarrierTrackingNumber ?? ClientOrderCode,
            now));

        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Marks the shipment as out for final delivery (last-mile). Idempotent: no-op
    /// if already Delivering or past that state.
    /// </summary>
    public UnitResult<e> MarkDelivering(DateTime now)
    {
        if (Status == OutboundShipmentStatus.Delivering ||
            Status == OutboundShipmentStatus.Delivered ||
            Status == OutboundShipmentStatus.Failed ||
            Status == OutboundShipmentStatus.Returning ||
            Status == OutboundShipmentStatus.Returned)
            return UnitResult.Success<e>();

        Status     = OutboundShipmentStatus.Delivering;
        ModifiedAt = now;

        RaiseDomainEvent(new OutboundShipmentDeliveringEvent(
            Id.ToString(), OrderId.ToString(), ProviderCode.Id, now));

        return UnitResult.Success<e>();
    }

    /// <summary>Called when carrier webhook reports delivery confirmed.</summary>
    public UnitResult<e> RecordDelivered(DateTime deliveredAt, DateTime now)
    {
        if (Status != OutboundShipmentStatus.InTransit &&
            Status != OutboundShipmentStatus.PickedUp &&
            Status != OutboundShipmentStatus.Delivering)
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
            { Id: "delivering" } => OutboundShipmentStatus.Delivering,
            { Id: "delivered" }  => OutboundShipmentStatus.Delivered,
            { Id: "failed" }     => OutboundShipmentStatus.Failed,
            { Id: "returning" }  => OutboundShipmentStatus.Returning,
            { Id: "returned" }   => OutboundShipmentStatus.Returned,
            _                    => Status
        };

        var oldStatusId = Status.Id;
        if (newStatus != Status)
        {
            Status     = newStatus;
            ModifiedAt = now;

            if (newStatus == OutboundShipmentStatus.PickedUp)
                DispatchedAt = eventTime;

            if (newStatus == OutboundShipmentStatus.Delivered)
                DeliveredAt = eventTime;
        }

        // Raise specific events based on status change to trigger Order system integration
        if (Status.Id != oldStatusId)
        {
            if (Status == OutboundShipmentStatus.PickedUp)
            {
                RaiseDomainEvent(new OutboundShipmentPickedUpEvent(
                    Id.ToString(), OrderId.ToString(), providerCode.Id, CarrierTrackingNumber ?? ClientOrderCode, now));
            }
            else if (Status == OutboundShipmentStatus.InTransit)
            {
                // Drives the Order lifecycle handler that transitions the
                // linked order to OnDelivering for warehouse-managed flows.
                RaiseDomainEvent(new OutboundShipmentInTransitEvent(
                    Id.ToString(), OrderId.ToString(), providerCode.Id, now));
            }
            else if (Status == OutboundShipmentStatus.Delivered)
            {
                RaiseDomainEvent(new OutboundShipmentDeliveredEvent(
                    Id.ToString(), OrderId.ToString(), providerCode.Id, eventTime, now));
            }
            else if (Status == OutboundShipmentStatus.Delivering)
            {
                RaiseDomainEvent(new OutboundShipmentDeliveringEvent(
                    Id.ToString(), OrderId.ToString(), providerCode.Id, now));
            }
            else if (Status == OutboundShipmentStatus.Failed)
            {
                RaiseDomainEvent(new OutboundShipmentFailedEvent(
                    Id.ToString(), OrderId.ToString(), reasonDescription ?? carrierStatusDesc ?? "Carrier reported failure", now));
            }
            else if (Status == OutboundShipmentStatus.Returning)
            {
                RaiseDomainEvent(new OutboundShipmentReturningEvent(
                    Id.ToString(), OrderId.ToString(), reasonDescription ?? carrierStatusDesc ?? "Carrier returning to sender", now));
            }
            else if (Status == OutboundShipmentStatus.Returned)
            {
                RaiseDomainEvent(new OutboundShipmentReturnedEvent(
                    Id.ToString(), OrderId.ToString(), now));
            }
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
