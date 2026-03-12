using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ShippingContext.Enums;

public sealed class InboundShipmentStatus : EnumValueObject<InboundShipmentStatus>
{
    public static readonly InboundShipmentStatus AwaitingPickup = new("awaiting_pickup");
    public static readonly InboundShipmentStatus InTransit = new("in_transit");
    public static readonly InboundShipmentStatus Arrived = new("arrived");
    public static readonly InboundShipmentStatus Inspected = new("inspected");
    public static readonly InboundShipmentStatus Completed = new("completed");
    public static readonly InboundShipmentStatus Cancelled = new("cancelled");
    public static readonly InboundShipmentStatus Failed = new("failed");
    private InboundShipmentStatus(string id) : base(id) { }
}