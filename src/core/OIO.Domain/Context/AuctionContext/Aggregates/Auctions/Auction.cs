using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Shared;
using System.Net;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class Auction : AggregateRoot<AuctionId>, IAuditableEntity
{
    private readonly List<AuctionPriceHistory> _priceHistories = [];
    private readonly List<AuctionDeposit> _deposits = [];
    private readonly List<AuctionAutoBid> _autoBids = [];
    private readonly List<AuctionWatcher> _watchers = [];

    // Properties từ Script DB
    public ItemId ItemId { get; private set; }
    public WinningConditions Conditions { get; private set; } 
    public Money CurrentPrice { get; private set; }           
    public BidIncrement Increment { get; private set; }      
    public AuctionPeriod Period { get; private set; }        
    public DateTime? ActualEndTime { get; private set; }     
    
    public AuctionStatus Status { get; private set; }        
    public Guid? CurrentWinnerId { get; private set; }       
    
    public bool AutoExtend { get; private set; }             
    public int ExtensionMinutes { get; private set; }        
    public bool IsFeatured { get; private set; }             
    
    public int ViewCount { get; private set; }               
    public int BidCount { get; private set; }                
    public int WatchCount { get; private set; }              

    public DateTime CreatedAt { get; private set; }          
    public DateTime? ModifiedAt { get; private set; }        

    // Navigations cho EF Core
    public IReadOnlyCollection<AuctionPriceHistory> PriceHistories => _priceHistories.AsReadOnly();
    public IReadOnlyCollection<AuctionDeposit> Deposits => _deposits.AsReadOnly();
    public IReadOnlyCollection<AuctionAutoBid> AutoBids => _autoBids.AsReadOnly();
    public IReadOnlyCollection<AuctionWatcher> Watchers => _watchers.AsReadOnly();

    private Auction() { }

    private Auction(
        AuctionId id,
        ItemId itemId,
        AuctionPeriod period,
        WinningConditions conditions,
        BidIncrement increment,
        bool autoExtend,
        int extensionMinutes,
        DateTime now)
    {
        Id = id;
        ItemId = itemId;
        Period = period;
        Conditions = conditions;
        Increment = increment;
        AutoExtend = autoExtend;
        ExtensionMinutes = extensionMinutes;
        
        Status = AuctionStatus.Draft;
        CurrentPrice = conditions.StartingPrice;
        
        BidCount = 0;
        ViewCount = 0;
        WatchCount = 0;
        IsFeatured = false;
        CreatedAt = now;
    }

    public static Result<Auction, Error> Create(
        ItemId itemId,
        AuctionPeriod period,
        WinningConditions conditions,
        BidIncrement increment,
        bool autoExtend,
        int extensionMinutes,
        DateTime now)
    {
        // Các check chéo giữa các Value Object (nếu có)
        return new Auction(
            AuctionId.From(Guid.CreateVersion7()), 
            itemId, period, conditions, increment, autoExtend, extensionMinutes, now);
    }

    public UnitResult<Error> PlaceBid(Guid bidderId, Money amount, BidId bidId, DateTime nowUtc)
    {
        if (Status != AuctionStatus.Active)
            return AuctionErrors.Auction.InvalidStatus;

        if (!Period.IsActive(nowUtc))
            return AuctionErrors.Auction.Expired;

        // Check tiền cọc nếu cần (Ví dụ: phải có record 'held' trong list Deposits)
        // if (!_deposits.Any(d => d.UserId == bidderId && d.Status == DepositStatus.Held))
        //     return AuctionErrors.Bid.DepositRequired;

        var minRequired = CurrentPrice.Amount + Increment.Value.Amount;
        if (amount.Amount < minRequired)
            return AuctionErrors.Bid.TooLow(minRequired);

        CurrentPrice = amount;
        CurrentWinnerId = bidderId;
        BidCount++;
        ModifiedAt = nowUtc;

        // Auto Extend logic
        if (AutoExtend && Period.EndTime.Subtract(nowUtc).TotalMinutes < ExtensionMinutes)
        {
            var extendedEndTime = nowUtc.AddMinutes(ExtensionMinutes);
            Period = AuctionPeriod.Create(Period.StartTime, extendedEndTime).Value;
        }

        // Lưu lịch sử giá (Sử dụng constructor mới đã fix)
        _priceHistories.Add(new AuctionPriceHistory(
            AuctionPriceHistoryId.From(Guid.CreateVersion7()),
            Id, amount, nowUtc, bidId));

        // Kiểm tra Buy Now
        if (Conditions.BuyNowPrice != null && amount.Amount >= Conditions.BuyNowPrice.Amount)
        {
            EndAuction(AuctionStatus.Ended, nowUtc, bidderId);
        }

        return UnitResult.Success<Error>();
    }

    public void AddDeposit(Guid userId, Money amount, Guid? transactionId, DateTime now)
    {
        var deposit = new AuctionDeposit(
            AuctionDepositId.From(Guid.CreateVersion7()),
            Id, userId, amount, transactionId, now);
        _deposits.Add(deposit);
    }

    public void AddWatcher(Guid userId, bool notifyOnBid, bool notifyOnEnd, DateTime now)
    {
        if (!_watchers.Any(w => w.UserId == userId))
        {
            _watchers.Add(new AuctionWatcher(
                AuctionWatcherId.From(Guid.CreateVersion7()),
                Id, userId, notifyOnBid, notifyOnEnd, now));
            WatchCount++;
        }
    }

    public void SetupAutoBid(Guid userId, Money maxAmount, Money currentAmount, Money? increment, DateTime now)
    {
        var existing = _autoBids.FirstOrDefault(b => b.BidderId == userId);
        if (existing != null) _autoBids.Remove(existing);

        _autoBids.Add(new AuctionAutoBid(
            AuctionAutoBidId.From(Guid.CreateVersion7()),
            Id, userId, maxAmount, currentAmount, increment, now));
    }

    public void EndAuction(AuctionStatus finalStatus, DateTime now, Guid? winnerId = null)
    {
        Status = finalStatus;
        ActualEndTime = now;
        if (winnerId.HasValue) CurrentWinnerId = winnerId;
        ModifiedAt = now;
    }

    public void IncrementView() => ViewCount++;
}