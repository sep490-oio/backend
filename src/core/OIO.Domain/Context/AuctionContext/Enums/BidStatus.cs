using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class BidStatus : EnumValueObject<BidStatus>
{
    public static readonly BidStatus Active = new("active");
    public static readonly BidStatus Outbid = new("outbid");
    public static readonly BidStatus Winning = new("winning");
    public static readonly BidStatus Won = new("won");
    public static readonly BidStatus Cancelled = new("cancelled");

    public BidStatus(string value) : base(value) { }
}