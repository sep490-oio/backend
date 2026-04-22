using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.OrderContext.Enums;

public sealed class ShippingFeePayer : EnumValueObject<ShippingFeePayer>
{
    public static readonly ShippingFeePayer Buyer    = new("buyer");
    public static readonly ShippingFeePayer Seller   = new("seller");
    public static readonly ShippingFeePayer Platform = new("platform");

    private ShippingFeePayer(string id) : base(id) { }
}
