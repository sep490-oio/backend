using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class ItemStatus : EnumValueObject<ItemStatus>
{
    public static readonly ItemStatus Draft = new("draft");
    public static readonly ItemStatus Active = new("active");
    public static readonly ItemStatus InAuction = new("in_auction");
    public static readonly ItemStatus Sold = new("sold");
    public static readonly ItemStatus Removed = new("removed");

    public ItemStatus(string value) : base(value) { }

    public bool CanTransitionTo(ItemStatus target)
    {
        return (this, target) switch
        {
            _ when this == Draft && target == Active => true,
            _ when this == Active && target == InAuction => true,
            _ when this == Active && target == Removed => true,
            _ when this == InAuction && target == Sold => true,
            _ when this == InAuction && target == Active => true, // Auction cancelled/failed
            _ when this == Sold && target == Removed => true,
            _ => false
        };
    }
}