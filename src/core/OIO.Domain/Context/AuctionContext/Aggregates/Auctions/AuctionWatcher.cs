using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionWatcher : BaseEntity<AuctionWatcherId>
{
    public AuctionId AuctionId { get; private set; }
    public Guid UserId { get; private set; }
    
    // Bổ sung các trường notify từ script DB
    public bool NotifyOnBid { get; private set; } // notify_on_bid
    public bool NotifyOnEnd { get; private set; } // notify_on_end
    
    public DateTime CreatedAt { get; private set; }

    private AuctionWatcher() { } // Dành cho EF Core

    internal AuctionWatcher(
        AuctionWatcherId id, 
        AuctionId auctionId, 
        Guid userId, 
        bool notifyOnBid, 
        bool notifyOnEnd, 
        DateTime now) 
        : base(id)
    {
        AuctionId = auctionId;
        UserId = userId;
        NotifyOnBid = notifyOnBid;
        NotifyOnEnd = notifyOnEnd;
        CreatedAt = now;
    }

    // Business methods để toggle thông báo
    public void UpdateNotificationSettings(bool notifyOnBid, bool notifyOnEnd)
    {
        NotifyOnBid = notifyOnBid;
        NotifyOnEnd = notifyOnEnd;
    }
}