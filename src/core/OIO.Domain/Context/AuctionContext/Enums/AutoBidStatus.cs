using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class AutoBidStatus : EnumValueObject<AutoBidStatus>
{
    public static readonly AutoBidStatus Active = new("active");
    public static readonly AutoBidStatus Paused = new("paused");
    public static readonly AutoBidStatus Exhausted = new("exhausted");
    public static readonly AutoBidStatus Won = new("won");
    public static readonly AutoBidStatus Outbid = new("outbid");

    public AutoBidStatus(string value) : base(value) { }
}