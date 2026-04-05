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
        InboundShipmentMode shipmentMode,
        string? externalCarrierName,
        DateTime now)
    {
        Id = id;
        ItemId = itemId;
        SellerId = sellerId;
        ProviderCode = providerCode;

        ShipmentMode = shipmentMode;
        ExternalCarrierName = externalCarrierName;

        ClientOrderCode = clientOrderCode;

        SenderName = senderName;
        SenderPhone = senderPhone;
        SenderAddress = senderAddress;
        SenderWard = senderWard;
        SenderDistrict = senderDistrict;
        SenderProvince = senderProvince;
        SenderCarrierAddressData = senderCarrierAddressData;

        Dimensions = dimensions;

        ShippingFee = shippingFee;
        InsuranceValue = insuranceValue;

        ExtraData = extraData;
        Notes = notes;

        Status = InboundShipmentStatus.AwaitingPickup;

        ExpectedArrivalAt = expectedArrivalAt;

        CreatedAt = now;
    }

    public Guid ItemId { get; private set; }

    public UserId SellerId { get; private set; }

    public ShippingProviderCode ProviderCode { get; private set; }

    public InboundShipmentMode ShipmentMode { get; private set; }

    public string? ExternalCarrierName { get; private set; }

    public string ClientOrderCode { get; private set; }

    public string? CarrierTrackingNumber { get; private set; }

    public string SenderName { get; private set; }

    public string SenderPhone { get; private set; }

    public string SenderAddress { get; private set; }

    public string SenderWard { get; private set; }

    public string SenderDistrict { get; private set; }

    public string SenderProvince { get; private set; }

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

    public static Result<InboundShipment, e> Create(
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
        InboundShipmentMode? shipmentMode = null,
        string? externalCarrierName = null,
        CarrierAddressData? senderCarrierAddressData = null,
        decimal shippingFee = 0,
        decimal insuranceValue = 0,
        ShipmentExtraData? extraData = null,
        string? notes = null,
        DateTime? expectedArrivalAt = null)
    {
        var mode = shipmentMode ?? InboundShipmentMode.PlatformManaged;

        if (mode == InboundShipmentMode.ExternalCarrier)
        {
            if (string.IsNullOrWhiteSpace(externalCarrierName))
                return WarehouseErrors.InboundShipment.ExternalCarrierNameRequired;

            if (providerCode != ShippingProviderCode.External)
                return WarehouseErrors.InboundShipment.NotExternalCarrier;
        }

        if (mode == InboundShipmentMode.PlatformManaged &&
            externalCarrierName is not null)
        {
            return WarehouseErrors.InboundShipment.NotPlatformManaged;
        }

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
            mode,
            externalCarrierName,
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

    public UnitResult<e> RecordBooked(string carrierTrackingNumber, DateTime now)
    {
        if (ShipmentMode == InboundShipmentMode.ExternalCarrier)
            return WarehouseErrors.InboundShipment.NotPlatformManaged;

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

    public UnitResult<e> SetExternalTrackingNumber(string trackingNumber, DateTime now)
    {
        if (ShipmentMode != InboundShipmentMode.ExternalCarrier)
            return WarehouseErrors.InboundShipment.NotExternalCarrier;

        if (CarrierTrackingNumber is not null)
            return WarehouseErrors.InboundShipment.ExternalTrackingAlreadySet;

        CarrierTrackingNumber = trackingNumber;

        ModifiedAt = now;

        return UnitResult.Success<e>();
    }

    public UnitResult<e> ManuallyAdvanceStatus(InboundShipmentStatus newStatus, DateTime now, bool isStaffRole = false)
    {
        if (ShipmentMode != InboundShipmentMode.ExternalCarrier)
            return WarehouseErrors.InboundShipment.NotExternalCarrier;

        if (newStatus.Id == Status.Id)
            return WarehouseErrors.InboundShipment.StatusUnchanged;

        // Seller transitions: AwaitingPickup → InTransit, InTransit → SellerClaimsArrived
        // Staff transitions:  AwaitingPickup → InTransit, InTransit → Arrived, SellerClaimsArrived → Arrived
        if (Status == InboundShipmentStatus.AwaitingPickup &&
            newStatus != InboundShipmentStatus.InTransit)
            return WarehouseErrors.InboundShipment.InvalidTransition;

        if (Status == InboundShipmentStatus.InTransit)
        {
            if (isStaffRole && newStatus == InboundShipmentStatus.Arrived)
            {
                // Staff can directly confirm arrival
            }
            else if (!isStaffRole && newStatus == InboundShipmentStatus.SellerClaimsArrived)
            {
                // Seller claims arrival — pending staff confirmation
            }
            else
            {
                return WarehouseErrors.InboundShipment.InvalidTransition;
            }
        }

        if (Status == InboundShipmentStatus.SellerClaimsArrived)
        {
            if (!isStaffRole || newStatus != InboundShipmentStatus.Arrived)
                return WarehouseErrors.InboundShipment.InvalidTransition;
        }

        Status = newStatus;

        ModifiedAt = now;

        if (newStatus == InboundShipmentStatus.Arrived)
        {
            ArrivedAt = now;
            RaiseDomainEvent(new InboundShipmentArrivedEvent(
                Id.ToString(),
                ProviderCode.Id,
                CarrierTrackingNumber ?? ClientOrderCode,
                now));
        }

        return UnitResult.Success<e>();
    }

    public UnitResult<e> RecordArrived(DateTime now)
    {
        if (Status == InboundShipmentStatus.Arrived)
            return WarehouseErrors.InboundShipment.AlreadyArrived;

        Status = InboundShipmentStatus.Arrived;

        ArrivedAt = now;

        ModifiedAt = now;

        RaiseDomainEvent(new InboundShipmentArrivedEvent(
            Id.ToString(),
            ProviderCode.Id,
            CarrierTrackingNumber ?? ClientOrderCode,
            now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> RecordInspected(UserId inspectedBy, DateTime now)
    {
        if (Status != InboundShipmentStatus.Arrived)
            return WarehouseErrors.InboundShipment.CannotInspect;

        Status = InboundShipmentStatus.Inspected;

        ModifiedAt = now;

        RaiseDomainEvent(new InboundShipmentInspectedEvent(
            Id.ToString(),
            ItemId.ToString(),
            string.Empty,
            inspectedBy.ToString(),
            now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> Complete(DateTime now)
    {
        if (Status != InboundShipmentStatus.Inspected && Status != InboundShipmentStatus.Arrived)
            return WarehouseErrors.InboundShipment.CannotComplete;

        Status = InboundShipmentStatus.Completed;

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

        Status = InboundShipmentStatus.Cancelled;

        ModifiedAt = now;

        RaiseDomainEvent(new InboundShipmentCancelledEvent(
            Id.ToString(),
            reason,
            now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> RecordFailed(string reason, DateTime now)
    {
        if (Status is { Id: "completed" or "cancelled" or "failed" })
            return WarehouseErrors.InboundShipment.CannotCancel;

        Status = InboundShipmentStatus.Failed;

        ModifiedAt = now;

        RaiseDomainEvent(new InboundShipmentFailedEvent(
            Id.ToString(),
            reason,
            now));

        return UnitResult.Success<e>();
    }

    public Result<ShipmentTrackingEvent, e> RecordTrackingEvent(
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
        if (ShipmentMode == InboundShipmentMode.ExternalCarrier)
            return WarehouseErrors.InboundShipment.TrackingNotAllowedForExternal;

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

        var oldStatusId = Status.Id;
        switch (normalizedStatus.Id)
        {
            case "picked_up" or "in_transit" or "delayed":
                if (Status == InboundShipmentStatus.AwaitingPickup ||
                    Status == InboundShipmentStatus.InTransit)
                {
                    Status = InboundShipmentStatus.InTransit;
                    ModifiedAt = now;
                }
                break;

            case "delivered":
                if (Status != InboundShipmentStatus.Arrived &&
                    Status != InboundShipmentStatus.Inspected &&
                    Status != InboundShipmentStatus.Completed)
                {
                    Status = InboundShipmentStatus.Arrived;
                    ArrivedAt = now;
                    ModifiedAt = now;
                }
                break;

            case "cancelled":
                if (Status is not { Id: "completed" or "cancelled" or "failed" })
                {
                    Status = InboundShipmentStatus.Cancelled;
                    ModifiedAt = now;
                }
                break;

            case "failed" or "returning" or "returned":
                if (Status is not { Id: "completed" or "cancelled" or "failed" })
                {
                    Status = InboundShipmentStatus.Failed;
                    ModifiedAt = now;
                }
                break;
        }

        // Raise specific events based on status change
        if (Status.Id != oldStatusId)
        {
            if (Status == InboundShipmentStatus.Arrived)
            {
                RaiseDomainEvent(new InboundShipmentArrivedEvent(
                    Id.ToString(), ProviderCode.Id, CarrierTrackingNumber ?? ClientOrderCode, now));
            }
            else if (Status == InboundShipmentStatus.Cancelled)
            {
                RaiseDomainEvent(new InboundShipmentCancelledEvent(
                    Id.ToString(), reasonDescription ?? carrierStatusDesc ?? "Carrier reported cancellation", now));
            }
            else if (Status == InboundShipmentStatus.Failed)
            {
                RaiseDomainEvent(new InboundShipmentFailedEvent(
                    Id.ToString(), reasonDescription ?? carrierStatusDesc ?? "Carrier reported failure", now));
            }
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