using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Aggregates.Bids.Events;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Shared;
using System.Net;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Bids;

public sealed class Bid : AggregateRoot<BidId>
{
    // Properties khớp 100% với Script DB
    public AuctionId AuctionId { get; private set; }
    public Guid BidderId { get; private set; }
    public Money Amount { get; private set; }
    public bool IsAutoBid { get; private set; }      // is_auto_bid
    public AuctionAutoBidId? AutoBidId { get; private set; }
    public BidStatus Status { get; private set; }    // status
    public IPAddress? IpAddress { get; private set; } // ip_address
    public DateTime CreatedAt { get; private set; }  // created_at

    private Bid() { }

    private Bid(
        BidId id, 
        AuctionId auctionId, 
        Guid bidderId, 
        Money amount, 
        bool isAutoBid, 
        AuctionAutoBidId? autoBidId, 
        IPAddress? ipAddress,
        DateTime now)
    {
        Id = id;
        AuctionId = auctionId;
        BidderId = bidderId;
        Amount = amount;
        IsAutoBid = isAutoBid;
        AutoBidId = autoBidId;
        IpAddress = ipAddress;
        Status = BidStatus.Active;
        CreatedAt = now;
    }

    public static Bid Create(
        AuctionId auctionId, 
        Guid bidderId, 
        Money amount, 
        bool isAutoBid,
        AuctionAutoBidId? autoBidId,
        IPAddress? ipAddress,
        DateTime now) // Đã sửa từ 'CreatedAt now' thành 'DateTime now'
    {
        var bid = new Bid(
            BidId.From(Guid.CreateVersion7()), 
            auctionId, 
            bidderId, 
            amount, 
            isAutoBid, 
            autoBidId, 
            ipAddress, 
            now);
        
        bid.RaiseDomainEvent(new BidPlacedDomainEvent(
            bid.Id.ToString(), 
            auctionId.ToString(), 
            bidderId, 
            amount, 
            now));

        return bid;
    }

    public void UpdateStatus(BidStatus newStatus)
    {
        // Vì DB không có modified_at, chúng ta chỉ cập nhật Status.
        // Không nên gán CreatedAt = now vì sẽ làm mất dấu thời gian đặt bid ban đầu.
        Status = newStatus;
    }
}