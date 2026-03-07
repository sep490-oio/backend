using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class ItemCondition : EnumValueObject<ItemCondition>
{
    public static readonly ItemCondition New = new("new");
    public static readonly ItemCondition LikeNew = new("like_new");
    public static readonly ItemCondition VeryGood = new("very_good");
    public static readonly ItemCondition Good = new("good");
    public static readonly ItemCondition Acceptable = new("acceptable");

    public ItemCondition(string value) : base(value) { }
}