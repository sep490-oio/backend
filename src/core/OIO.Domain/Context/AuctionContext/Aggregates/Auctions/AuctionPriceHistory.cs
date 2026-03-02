using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionPriceHistory : BaseEntity<AuctionPriceHistoryId>
{
    public AuctionId AuctionId { get; private set; }
    public Guid BidderId { get; private set; }
    public Money Amount { get; private set; }
    
    // Thêm BidId để khớp với cột "bid_id" trong database
    public BidId? BidId { get; private set; } 
    public DateTime CreatedAt { get; private set; }

    // Constructor dùng cho EF Core
    private AuctionPriceHistory() { }

    internal AuctionPriceHistory(
        AuctionPriceHistoryId id, 
        AuctionId auctionId, 
        Guid bidderId, 
        Money amount, 
        DateTime now,
        BidId? bidId = null) // Chấp nhận bidId truyền vào
        : base(id)
    {
        AuctionId = auctionId;
        BidderId = bidderId;
        Amount = amount;
        CreatedAt = now;
        BidId = bidId;
    }
}