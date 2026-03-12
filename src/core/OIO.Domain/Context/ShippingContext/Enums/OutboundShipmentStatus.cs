using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ShippingContext.Enums;

public sealed class OutboundShipmentStatus : EnumValueObject<OutboundShipmentStatus>
{
    public static readonly OutboundShipmentStatus Pending = new("pending");
    public static readonly OutboundShipmentStatus Booked = new("booked");
    public static readonly OutboundShipmentStatus PickedUp = new("picked_up");
    public static readonly OutboundShipmentStatus InTransit = new("in_transit");
    public static readonly OutboundShipmentStatus Delivered = new("delivered");
    public static readonly OutboundShipmentStatus Failed = new("failed");
    public static readonly OutboundShipmentStatus Returning = new("returning");
    public static readonly OutboundShipmentStatus Returned = new("returned");
    public static readonly OutboundShipmentStatus Cancelled = new("cancelled");
    private OutboundShipmentStatus(string id) : base(id) { }
}