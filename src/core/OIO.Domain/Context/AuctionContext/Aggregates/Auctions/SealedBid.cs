using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class SealedBid : BaseEntity<SealedBidId>, ICreatedAtEntity
{
    public AuctionId AuctionId { get; private set; }
    public UserId BidderId { get; private set; }
    public string AmountEncrypted { get; private set; }  // text
    public SealedBidStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevealedAt { get; private set; }
    public UserId? RevealedBy { get; private set; }

    // Navigation
    public Auction Auction { get; private set; } = null!;

    private SealedBid() { }
}