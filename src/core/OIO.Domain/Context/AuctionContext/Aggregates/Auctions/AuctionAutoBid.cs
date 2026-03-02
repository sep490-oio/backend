using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionAutoBid : BaseEntity<AuctionAutoBidId>
{
    public AuctionId AuctionId { get; private set; }
    public Guid UserId { get; private set; }
    public Money MaxAmount { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    internal AuctionAutoBid(AuctionAutoBidId id, AuctionId auctionId, Guid userId, Money maxAmount, DateTime now) 
        : base(id)
    {
        AuctionId = auctionId;
        UserId = userId;
        MaxAmount = maxAmount;
        IsActive = true;
        CreatedAt = now;
    }

    public void Deactivate() => IsActive = false;
}