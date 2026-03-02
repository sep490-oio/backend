using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionDeposit : BaseEntity<AuctionDepositId>
{
    public AuctionId AuctionId { get; private set; }
    public Guid UserId { get; private set; }
    public Money Amount { get; private set; }
    public DepositStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    internal AuctionDeposit(AuctionDepositId id, AuctionId auctionId, Guid userId, Money amount, DateTime now) 
        : base(id)
    {
        AuctionId = auctionId;
        UserId = userId;
        Amount = amount;
        Status = DepositStatus.Pending;
        CreatedAt = now;
    }

    public void Confirm() => Status = DepositStatus.Confirmed;
}