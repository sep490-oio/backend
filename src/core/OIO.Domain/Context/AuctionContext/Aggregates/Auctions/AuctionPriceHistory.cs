using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionPriceHistory : BaseEntity<AuctionPriceHistoryId>
{
    public AuctionId AuctionId { get; private set; }
    public Money Price { get; private set; } // Khớp cột "price"
    public BidId? BidId { get; private set; } // Khớp cột "bid_id"
    public DateTime RecordedAt { get; private set; } // Khớp cột "recorded_at"

    // Constructor dùng cho EF Core
    private AuctionPriceHistory() { }

    internal AuctionPriceHistory(
        AuctionPriceHistoryId id, 
        AuctionId auctionId, 
        Money price, 
        DateTime now,
        BidId? bidId = null) 
        : base(id)
    {
        AuctionId = auctionId;
        Price = price;
        RecordedAt = now;
        BidId = bidId;
    }
}