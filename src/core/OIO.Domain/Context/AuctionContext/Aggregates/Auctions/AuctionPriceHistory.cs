using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionPriceHistory : BaseEntity<AuctionPriceHistoryId>
{
    public AuctionId AuctionId { get; private set; }
    public Money Price { get; private set; }
    public BidId? BidId { get; private set; }
    public DateTime RecordedAt { get; private set; }

    private AuctionPriceHistory() { }

    private AuctionPriceHistory(
        AuctionPriceHistoryId id,
        AuctionId auctionId,
        Money price,
        DateTime recordedAt,
        BidId? bidId = null)
        : base(id)
    {
        AuctionId = auctionId;
        Price = price;
        BidId = bidId;
        RecordedAt = recordedAt;
    }
    
    public static AuctionPriceHistory Create(
        AuctionId auctionId,
        Money price,
        DateTime recordedAt,
        BidId? bidId = null)
    {
        return new AuctionPriceHistory(
            AuctionPriceHistoryId.From(Guid.CreateVersion7()),
            auctionId,
            price,
            recordedAt,
            bidId);
    }
}