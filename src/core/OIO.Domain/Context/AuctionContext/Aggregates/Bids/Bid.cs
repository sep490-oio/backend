using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Aggregates.Bids.Events;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Bids;

public sealed class Bid : AggregateRoot<BidId>, IAuditableEntity, IVersionEntity
{
    public AuctionId AuctionId { get; private set; }
    public Guid BidderId { get; private set; }
    public Money Amount { get; private set; }
    public BidStatus Status { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    public int Version { get; private set; }

    private Bid() { }

    private Bid(BidId id, AuctionId auctionId, Guid bidderId, Money amount, DateTime now)
    {
        Id = id;
        AuctionId = auctionId;
        BidderId = bidderId;
        Amount = amount;
        Status = BidStatus.Active;
        CreatedAt = now;
    }

    public static Bid Create(AuctionId auctionId, Guid bidderId, Money amount, DateTime now)
    {
        var bid = new Bid(BidId.From(Guid.CreateVersion7()), auctionId, bidderId, amount, now);
        
        bid.RaiseDomainEvent(new BidPlacedDomainEvent(
            bid.Id.ToString(), 
            auctionId.ToString(), 
            bidderId, 
            amount, 
            now));

        return bid;
    }

    public void MarkAsOutbid(DateTime now)
    {
        if (Status == BidStatus.Active)
        {
            Status = BidStatus.Outbid;
            ModifiedAt = now;
            RaiseDomainEvent(new BidOutbidDomainEvent(Id.ToString(), AuctionId.ToString(), BidderId, now));
        }
    }
}