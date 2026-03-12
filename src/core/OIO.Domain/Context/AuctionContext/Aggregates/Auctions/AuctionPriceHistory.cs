using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionPriceHistory : BaseEntity<AuctionPriceHistoryId>, ICreatedAtEntity
{
    public AuctionId AuctionId { get; private set; }
    public Money Price { get; private set; }
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
        DateTime createdAt,
        BidId? bidId = null)
        : base(id)
    {
        AuctionId = auctionId;
        Price = price;
        BidId = bidId;
        CreatedAt = createdAt;
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