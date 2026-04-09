using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class DisputeDomain : EnumValueObject<DisputeDomain>
{
    public static readonly DisputeDomain AuctionSettlement = new("auction_settlement");
    public static readonly DisputeDomain ItemCondition = new("item_condition");
    public static readonly DisputeDomain Payment = new("payment");
    public static readonly DisputeDomain Shipping = new("shipping");
    private DisputeDomain(string id) : base(id) { }
}
