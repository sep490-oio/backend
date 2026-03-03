using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionAutoBid : BaseEntity<AuctionAutoBidId>
{
    // Properties khớp 100% với Script DB
    public AuctionId AuctionId { get; private set; }
    public Guid BidderId { get; private set; }         // bidder_id
    public bool IsEnabled { get; private set; }        // is_enabled
    public Money MaxAmount { get; private set; }       // max_amount
    public Money CurrentAmount { get; private set; }   // current_amount
    public Money? IncrementAmount { get; private set; } // increment_amount
    public AutoBidStatus Status { get; private set; }  // status (active, exhausted, etc.)
    public int TotalAutoBids { get; private set; }     // total_auto_bids
    
    public DateTime? LastAutoBidAt { get; private set; } // last_auto_bid_at
    public DateTime CreatedAt { get; private set; }     // created_at
    public DateTime? ModifiedAt { get; private set; }   // modified_at

    private AuctionAutoBid() { }

    internal AuctionAutoBid(
        AuctionAutoBidId id, 
        AuctionId auctionId, 
        Guid bidderId, 
        Money maxAmount, 
        Money currentAmount,
        Money? incrementAmount,
        DateTime now) 
        : base(id)
    {
        AuctionId = auctionId;
        BidderId = bidderId;
        MaxAmount = maxAmount;
        CurrentAmount = currentAmount;
        IncrementAmount = incrementAmount;
        IsEnabled = true;
        Status = AutoBidStatus.Active;
        TotalAutoBids = 0;
        CreatedAt = now;
    }

    // --- Business Methods ---

    public void UpdateCurrentAmount(Money newAmount, DateTime now)
    {
        CurrentAmount = newAmount;
        TotalAutoBids++;
        LastAutoBidAt = now;
        ModifiedAt = now;

        if (CurrentAmount.Amount >= MaxAmount.Amount)
        {
            Status = AutoBidStatus.Exhausted;
            IsEnabled = false;
        }
    }

    public void Pause()
    {
        Status = AutoBidStatus.Paused;
        IsEnabled = false;
    }

    public void Resume()
    {
        Status = AutoBidStatus.Active;
        IsEnabled = true;
    }

    public void MarkAsWon() => Status = AutoBidStatus.Won;
    public void MarkAsOutbid() => Status = AutoBidStatus.Outbid;
}