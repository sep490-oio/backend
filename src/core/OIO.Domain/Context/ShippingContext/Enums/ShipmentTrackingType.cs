using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ShippingContext.Enums;

public sealed class ShipmentTrackingType : EnumValueObject<ShipmentTrackingType>
{
    public static readonly ShipmentTrackingType Inbound = new("inbound");
    public static readonly ShipmentTrackingType Outbound = new("outbound");
    private ShipmentTrackingType(string id) : base(id) { }
}