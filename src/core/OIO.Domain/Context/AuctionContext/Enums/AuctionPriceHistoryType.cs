using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.AuctionContext.Enums;

public sealed class AuctionPriceHistoryType : EnumValueObject<AuctionPriceHistoryType>
{
    public static readonly AuctionPriceHistoryType Unknown = new("unknown");
    public static readonly AuctionPriceHistoryType StartingPrice = new("starting_price");
    public static readonly AuctionPriceHistoryType Bid = new("bid");
    public static readonly AuctionPriceHistoryType AutoBid = new("auto_bid");
    public static readonly AuctionPriceHistoryType BuyNow = new("buy_now");
    public static readonly AuctionPriceHistoryType SealedBid = new("sealed_bid");
    public static readonly AuctionPriceHistoryType ResetToStartingPrice = new("reset_to_starting_price");
    public static readonly AuctionPriceHistoryType RepricedAfterBidCancellation = new("repriced_after_bid_cancellation");

    private AuctionPriceHistoryType(string id) : base(id) { }
}
