using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionWatcher : BaseEntity<AuctionWatcherId>, ICreatedAtEntity
{
    public AuctionId AuctionId { get; private set; }
    public UserId UserId { get; private set; }
    public bool NotifyOnBid { get; private set; }
    public bool NotifyOnEnd { get; private set; } 
    public DateTime CreatedAt { get; private set; }
    
    public Auction Auction { get; private set; } = null!;

    private AuctionWatcher() { }

    internal AuctionWatcher(
        AuctionWatcherId id, 
        AuctionId auctionId, 
        UserId userId, 
        DateTime nowUtc, 
        bool notifyOnBid = true, 
        bool notifyOnEnd = true) 
        : base(id)
    {
        AuctionId = auctionId;
        UserId = userId;
        NotifyOnBid = notifyOnBid;
        NotifyOnEnd = notifyOnEnd;
        CreatedAt = nowUtc;
    }
    
    public static AuctionWatcher Create(
        AuctionId auctionId, 
        UserId userId, 
        DateTime nowUtc, 
        bool notifyOnBid = true, 
        bool notifyOnEnd = true)
    {
        return new AuctionWatcher(
            AuctionWatcherId.From(Guid.CreateVersion7()), 
            auctionId, 
            userId, 
            nowUtc, 
            notifyOnBid, 
            notifyOnEnd);
    }

    public void UpdateNotificationSettings(bool? notifyOnBid, bool? notifyOnEnd)
    {
        NotifyOnBid = notifyOnBid ?? NotifyOnBid;
        NotifyOnEnd = notifyOnEnd ??  NotifyOnEnd;
    }
}