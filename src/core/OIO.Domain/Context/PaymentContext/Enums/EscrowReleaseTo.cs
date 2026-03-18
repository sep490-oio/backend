using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.PaymentContext.Enums;

public sealed class EscrowReleaseTo : EnumValueObject<EscrowReleaseTo>
{
    public static readonly EscrowReleaseTo None = new("none");
    public static readonly EscrowReleaseTo Platform = new("platform");
    public static readonly EscrowReleaseTo Seller = new("seller");
    public static readonly EscrowReleaseTo Buyer = new("buyer");
    private EscrowReleaseTo(string id) : base(id) { }
}