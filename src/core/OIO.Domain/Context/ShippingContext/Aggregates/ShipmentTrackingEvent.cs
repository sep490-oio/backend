using OIO.Domain.Context.ShippingContext.Enums;
using OIO.Domain.Context.ShippingContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ShippingContext.Aggregates;

public sealed class ShipmentTrackingEvent : BaseEntity<ShipmentTrackingEventId>, ICreatedAtEntity
{
    public ShipmentTrackingType ShipmentType { get; private set; }
    public Guid ShipmentId { get; private set; }  // polymorphic FK
    public string ProviderCode { get; private set; }
    public string CarrierStatusRaw { get; private set; }
    public string? CarrierStatusDesc { get; private set; }
    public string NormalizedStatus { get; private set; }
    public string? Location { get; private set; }
    public string? ReasonCode { get; private set; }
    public string? ReasonDescription { get; private set; }
    public DateTime EventTime { get; private set; }
    public string RawPayload { get; private set; }  // jsonb
    public DateTime CreatedAt { get; private set; }

    private ShipmentTrackingEvent() { }
}