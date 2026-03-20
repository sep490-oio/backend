using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionRelistHistory : BaseEntity<AuctionRelistHistoryId>, ICreatedAtEntity
{
    public AuctionId AuctionId { get; private set; }
    public AuctionId? NewAuctionId { get; private set; }
    public int RelistNo { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Auction Auction { get; private set; } = null!;

    private AuctionRelistHistory() { }

    public static AuctionRelistHistory Create(
        AuctionId auctionId,
        int relistNo,
        string? reason,
        DateTime createdAt,
        AuctionId? newAuctionId = null)
    {
        return new AuctionRelistHistory
        {
            Id = AuctionRelistHistoryId.From(Guid.CreateVersion7()),
            AuctionId = auctionId,
            NewAuctionId = newAuctionId,
            RelistNo = relistNo,
            Reason = reason,
            CreatedAt = createdAt
        };
    }
}
