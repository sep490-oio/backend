using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionPriceHistory : BaseEntity<AuctionPriceHistoryId>, ICreatedAtEntity
{
    public AuctionId AuctionId { get; private set; }
    public Money Price { get; private set; }
    public AuctionPriceHistoryType Type { get; private set; } = AuctionPriceHistoryType.Unknown;
    public BidId? BidId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Auction Auction { get; private set; } = null!;
    public Bid? Bid { get; private set; }

    private AuctionPriceHistory() { }

    private AuctionPriceHistory(
        AuctionPriceHistoryId id,
        AuctionId auctionId,
        Money price,
        AuctionPriceHistoryType type,
        DateTime createdAt,
        BidId? bidId = null)
        : base(id)
    {
        AuctionId = auctionId;
        Price = price;
        Type = type;
        BidId = bidId;
        CreatedAt = createdAt;
    }

    public static AuctionPriceHistory Create(
        AuctionId auctionId,
        Money price,
        AuctionPriceHistoryType type,
        DateTime recordedAt,
        BidId? bidId = null)
    {
        return new AuctionPriceHistory(
            AuctionPriceHistoryId.From(Guid.CreateVersion7()),
            auctionId,
            price,
            type,
            recordedAt,
            bidId);
    }

    public static AuctionPriceHistory CreateStartingPrice(
        AuctionId auctionId,
        Money price,
        DateTime recordedAt)
    {
        return Create(auctionId, price, AuctionPriceHistoryType.StartingPrice, recordedAt);
    }

    public static AuctionPriceHistory CreateBid(
        AuctionId auctionId,
        Money price,
        DateTime recordedAt,
        BidId bidId)
    {
        return Create(auctionId, price, AuctionPriceHistoryType.Bid, recordedAt, bidId);
    }

    public static AuctionPriceHistory CreateBuyNow(
        AuctionId auctionId,
        Money price,
        DateTime recordedAt,
        BidId bidId)
    {
        return Create(auctionId, price, AuctionPriceHistoryType.BuyNow, recordedAt, bidId);
    }

    public static AuctionPriceHistory CreateSealedBid(
        AuctionId auctionId,
        Money price,
        DateTime recordedAt,
        BidId bidId)
    {
        return Create(auctionId, price, AuctionPriceHistoryType.SealedBid, recordedAt, bidId);
    }

    public static AuctionPriceHistory CreateResetToStartingPrice(
        AuctionId auctionId,
        Money price,
        DateTime recordedAt)
    {
        return Create(auctionId, price, AuctionPriceHistoryType.ResetToStartingPrice, recordedAt);
    }

    public static AuctionPriceHistory CreateRepricedAfterBidCancellation(
        AuctionId auctionId,
        Money price,
        DateTime recordedAt,
        BidId bidId)
    {
        return Create(auctionId, price, AuctionPriceHistoryType.RepricedAfterBidCancellation, recordedAt, bidId);
    }
}
