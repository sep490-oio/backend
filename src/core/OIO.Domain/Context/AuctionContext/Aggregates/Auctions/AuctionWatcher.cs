using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionWatcher : BaseEntity<AuctionWatcherId>
{
    public AuctionId AuctionId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    internal AuctionWatcher(AuctionWatcherId id, AuctionId auctionId, Guid userId, DateTime now) 
        : base(id)
    {
        AuctionId = auctionId;
        UserId = userId;
        CreatedAt = now;
    }
}