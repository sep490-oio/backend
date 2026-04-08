using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.OrderContext.Enums;

public sealed class SellerDirectShipmentStatus : EnumValueObject<SellerDirectShipmentStatus>
{
    public static readonly SellerDirectShipmentStatus Draft = new("draft");
    public static readonly SellerDirectShipmentStatus CarrierBooked = new("carrier_booked");
    public static readonly SellerDirectShipmentStatus PickedUp = new("picked_up");
    public static readonly SellerDirectShipmentStatus OnDelivering = new("on_delivering");
    public static readonly SellerDirectShipmentStatus Delivered = new("delivered");
    public static readonly SellerDirectShipmentStatus Accepted = new("accepted");
    public static readonly SellerDirectShipmentStatus Disputed = new("disputed");
    public static readonly SellerDirectShipmentStatus Completed = new("completed");

    private SellerDirectShipmentStatus(string id) : base(id) { }
}
