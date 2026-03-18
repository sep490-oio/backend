using System.Net;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class Bid : BaseEntity<BidId>, ICreatedAtEntity
{
    private readonly List<BidEvent> _bidEvents = [];
    
    public AuctionId AuctionId { get; private set; }
    public UserId BidderId { get; private set; }
    public Money Amount { get; private set; }
    public AutoBidId? AutoBidId { get; private set; }
    public bool IsAutoBid { get; private set; }      // is_auto_bid
    public BidStatus Status { get; private set; }    // status
    public IPAddress? IpAddress { get; private set; } // ip_address
    public DateTime CreatedAt { get; private set; }  // created_at
    
    // Navigation
    public Auction Auction { get; private set; } = null!;
    public AutoBid? AutoBid { get; private set; }
    public IReadOnlyCollection<BidEvent> BidEvents => _bidEvents.AsReadOnly();

    private Bid() { }

    private Bid(
        BidId id, 
        AuctionId auctionId, 
        UserId bidderId, 
        Money amount, 
        AutoBidId? autoBidId, 
        IPAddress? ipAddress,
        DateTime nowUtc)
    {
        Id = id;
        AuctionId = auctionId;
        BidderId = bidderId;
        Amount = amount;
        IsAutoBid = autoBidId.HasValue;
        AutoBidId = autoBidId;
        IpAddress = ipAddress;
        Status = BidStatus.Active;
        CreatedAt = nowUtc;
    }

    public static Bid Create(
        AuctionId auctionId, 
        UserId bidderId, 
        Money amount, 
        AutoBidId? autoBidId,
        IPAddress? ipAddress,
        DateTime nowUtc,
        DateTime? createdAt = null) 
    {
        var bid = new Bid(
            BidId.From(Guid.CreateVersion7()), 
            auctionId, 
            bidderId, 
            amount, 
            autoBidId, 
            ipAddress, 
            createdAt ?? nowUtc);

        return bid;
    }

    internal void MarkAsOutbid() => Status = BidStatus.Outbid;
    internal void MarkAsWinning() => Status = BidStatus.Winning;
    internal void MarkAsWon() => Status = BidStatus.Won;
    internal void Cancel() => Status = BidStatus.Cancelled;
}
