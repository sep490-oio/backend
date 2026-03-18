using CSharpFunctionalExtensions;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.WarehouseContext.Aggregates;

/// <summary>
/// Append-only tracking event pushed via carrier webhook.
/// Shared by both InboundShipment and OutboundShipment via polymorphic
/// shipment_type + shipment_id columns.
///
/// carrier_status_raw: exactly what the carrier sent — never transform before storing.
///   GHN:  strings  → "ready_to_pick", "delivering", "delivered"
///   GHTK: integers → "2", "4", "5" (serialized to string in adapter)
///
/// normalized_status: our own enum, mapped in adapter layer before creation.
/// </summary>
public sealed class ShipmentTrackingEvent : Entity<ShipmentTrackingEventId>
{
    private ShipmentTrackingEvent() { }

    internal ShipmentTrackingEvent(
        ShipmentTrackingEventId id,
        string shipmentType,
        Guid shipmentId,
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
        Id                  = id;
        ShipmentType        = shipmentType;
        ShipmentId          = shipmentId;
        ProviderCode        = providerCode;
        CarrierStatusRaw    = carrierStatusRaw;
        CarrierStatusDesc   = carrierStatusDesc;
        NormalizedStatus    = normalizedStatus;
        Location            = location;
        ReasonCode          = reasonCode;
        ReasonDescription   = reasonDescription;
        EventTime           = eventTime;
        RawPayload          = rawPayload;
        CreatedAt           = now;
    }

    /// <summary>"inbound" or "outbound" — discriminates which table shipment_id references.</summary>
    public string ShipmentType { get; private set; }

    /// <summary>FK to either inbound_shipments.id or outbound_shipments.id.</summary>
    public Guid ShipmentId { get; private set; }

    public ShippingProviderCode ProviderCode { get; private set; }

    /// <summary>
    /// Exactly what the carrier sent. Never transform before storing.
    /// GHN: string e.g. "ready_to_pick". GHTK: integer as string e.g. "5".
    /// </summary>
    public string CarrierStatusRaw { get; private set; }

    public string? CarrierStatusDesc { get; private set; }

    /// <summary>Our own normalized status, mapped from carrier raw in adapter.</summary>
    public NormalizedTrackingStatus NormalizedStatus { get; private set; }

    /// <summary>
    /// GHTK: cur_station serialized (address_l0–l3 + site_name).
    /// GHN: hub or warehouse name.
    /// </summary>
    public string? Location { get; private set; }

    public string? ReasonCode { get; private set; }
    public string? ReasonDescription { get; private set; }

    /// <summary>Carrier's action_time — not our received time.</summary>
    public DateTime EventTime { get; private set; }

    /// <summary>
    /// Full webhook body. GHTK sends application/x-www-form-urlencoded —
    /// adapter serializes to JSON before creating this event.
    /// </summary>
    public WebhookRawPayload RawPayload { get; private set; }

    public DateTime CreatedAt { get; private set; }
}