using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionViewRecord : BaseEntity<AuctionViewRecordId>
{
    public AuctionId AuctionId { get; private set; }
    public UserId? UserId { get; private set; }
    public string? BrowserViewerId { get; private set; }
    public string? IpHash { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastSeenAt { get; private set; }

    private AuctionViewRecord() { }

    public static AuctionViewRecord Create(
        AuctionId auctionId,
        UserId? userId,
        string? browserViewerId,
        string? ipHash,
        DateTime now)
    {
        return new AuctionViewRecord
        {
            Id = AuctionViewRecordId.From(Guid.CreateVersion7()),
            AuctionId = auctionId,
            UserId = userId,
            BrowserViewerId = browserViewerId,
            IpHash = ipHash,
            CreatedAt = now,
            LastSeenAt = now
        };
    }

    public void MarkSeen(DateTime now) => LastSeenAt = now;

    public void MergeUserId(UserId userId)
    {
        if (UserId is null)
            UserId = userId;
    }
}
