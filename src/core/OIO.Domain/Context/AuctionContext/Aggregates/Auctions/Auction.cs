using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.SeedWork.Shared;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class Auction : AggregateRoot<AuctionId>, IAuditableEntity
{
    private readonly List<AuctionPriceHistory> _priceHistories = [];
    private readonly List<AuctionDeposit> _deposits = [];
    private readonly List<AuctionAutoBid> _autoBids = [];
    private readonly List<AuctionWatcher> _watchers = [];

    public ItemId ItemId { get; private set; }
    public UserId SellerId { get; private set; }
    public WinningConditions Conditions { get; private set; }
    public Money CurrentPrice { get; private set; }
    public BidIncrement Increment { get; private set; }
    public AuctionPeriod Period { get; private set; }
    public DateTime? ActualEndTime { get; private set; }

    public AuctionStatus Status { get; private set; }
    public UserId? CurrentWinnerId { get; private set; }

    public bool AutoExtend { get; private set; }
    public int ExtensionMinutes { get; private set; }
    public bool IsFeatured { get; private set; }

    public int ViewCount { get; private set; }
    public int BidCount { get; private set; }
    public int WatchCount { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

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
        UserId sellerId,
        bool autoExtend,
        int extensionMinutes,
        DateTime now)
        : base(id)
    {
        ItemId = itemId;
        Period = period;
        Conditions = conditions;
        Increment = increment;
        SellerId = sellerId;
        AutoExtend = autoExtend;
        ExtensionMinutes = extensionMinutes;

        Status = AuctionStatus.Draft;
        CurrentPrice = conditions.StartingPrice;
        CurrentWinnerId = null;
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
        UserId sellerId,
        bool autoExtend,
        int extensionMinutes,
        DateTime now)
    {
        if (itemId.Value == Guid.Empty)
            return AuctionErrors.Auction.InvalidInput("ItemId cannot be empty");

        if (sellerId.Value == Guid.Empty)
            return AuctionErrors.Auction.InvalidInput("SellerId cannot be empty");

        if (extensionMinutes < 0)
            return AuctionErrors.Auction.InvalidInput("ExtensionMinutes cannot be negative");

        var auction = new Auction(
            AuctionId.From(Guid.CreateVersion7()),
            itemId,
            period,
            conditions,
            increment,
            sellerId,
            autoExtend,
            extensionMinutes,
            now);

        auction.RaiseDomainEvent(new AuctionCreatedEvent(
            auction.Id.Value.ToString(),
            itemId.Value.ToString(),
            sellerId.Value.ToString(),
            now));

        return Result.Success<Auction, Error>(auction);
    }

    public UnitResult<Error> ActivateAuction(DateTime now)
    {
        if (Status != AuctionStatus.Draft)
            return AuctionErrors.Auction.InvalidStatus;

        Status = AuctionStatus.Active;
        ModifiedAt = now;
        RaiseDomainEvent(new AuctionActivatedEvent(Id.Value.ToString(), now));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> PlaceBid(UserId bidderId, Money amount, BidId bidId, DateTime nowUtc)
    {
        if (Status != AuctionStatus.Active)
            return AuctionErrors.Auction.InvalidStatus;

        if (!Period.IsActive(nowUtc))
            return AuctionErrors.Auction.Expired;

        if (bidderId == SellerId)
            return AuctionErrors.Auction.SelfBid;

        var minRequired = CurrentPrice.Amount + Increment.Value.Amount;
        if (amount.Amount < minRequired)
            return AuctionErrors.Bid.TooLow(minRequired);

        CurrentPrice = amount;
        CurrentWinnerId = bidderId;
        BidCount++;
        ModifiedAt = nowUtc;

        // Auto extend logic
        if (AutoExtend && Period.EndTime.Subtract(nowUtc).TotalMinutes < ExtensionMinutes)
        {
            var extendedEndTime = nowUtc.AddMinutes(ExtensionMinutes);
            var newPeriod = AuctionPeriod.Create(Period.StartTime, extendedEndTime);

            if (newPeriod.IsSuccess)
            {
                Period = newPeriod.Value;
                RaiseDomainEvent(new AuctionAutoExtendedEvent(
                    Id.Value.ToString(),
                    newPeriod.Value.EndTime,
                    nowUtc));
            }
        }

        // Record price history
        var priceHistoryResult = AuctionPriceHistory.Create(Id, amount, nowUtc, bidId);
        if (priceHistoryResult.IsFailure)
            return priceHistoryResult.Error;

        _priceHistories.Add(priceHistoryResult.Value);

        // Check buy now
        if (Conditions.BuyNowPrice != null && amount.Amount >= Conditions.BuyNowPrice.Amount)
        {
            EndAuction(AuctionStatus.Sold, nowUtc, bidderId);
            RaiseDomainEvent(new AuctionBoughtOutEvent(
                Id.Value.ToString(),
                bidderId.Value.ToString(),
                amount.Amount,
                nowUtc));
            return UnitResult.Success<Error>();
        }

        RaiseDomainEvent(new BidPlacedEvent(
            Id.Value.ToString(),
            bidderId.Value.ToString(),
            amount.Amount,
            bidId.Value.ToString(),
            nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> AddDeposit(UserId userId, Money amount, TransactionId? transactionId, DateTime now)
    {
        // Check if deposit already exists
        if (_deposits.Any(d => d.UserId == userId && d.IsHeld))
            return AuctionErrors.Deposit.AlreadyExists;

        var depositResult = AuctionDeposit.Create(Id, userId, amount, transactionId, now);
        if (depositResult.IsFailure)
            return depositResult.Error;

        _deposits.Add(depositResult.Value);
        RaiseDomainEvent(new DepositAddedEvent(
            Id.Value.ToString(),
            userId.Value.ToString(),
            amount.Amount,
            now));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> AddWatcher(UserId userId, bool notifyOnBid, bool notifyOnEnd, DateTime now)
    {
        if (_watchers.Any(w => w.UserId == userId))
            return AuctionErrors.Watcher.AlreadyWatching;

        if (userId == SellerId)
            return AuctionErrors.Watcher.CannotWatchOwnAuction;

        _watchers.Add(new AuctionWatcher(
            AuctionWatcherId.From(Guid.CreateVersion7()),
            Id, userId, notifyOnBid, notifyOnEnd, now));
        WatchCount++;
        RaiseDomainEvent(new AuctionWatcherAddedEvent(
            Id.Value.ToString(),
            userId.Value.ToString(),
            now));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> SetupAutoBid(UserId userId, Money maxAmount, Money currentAmount, Money? increment, DateTime now)
    {
        var autoBidResult = AuctionAutoBid.Create(Id, userId, maxAmount, currentAmount, increment, now);
        if (autoBidResult.IsFailure)
            return autoBidResult.Error;

        var existing = _autoBids.FirstOrDefault(b => b.BidderId == userId);
        if (existing != null)
        {
            _autoBids.Remove(existing);
            RaiseDomainEvent(new AutoBidReplacedEvent(
                Id.Value.ToString(),
                userId.Value.ToString(),
                now));
        }

        _autoBids.Add(autoBidResult.Value);
        RaiseDomainEvent(new AutoBidSetupEvent(
            Id.Value.ToString(),
            userId.Value.ToString(),
            maxAmount.Amount,
            now));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> CancelAuction(string reason, DateTime now)
    {
        if (Status == AuctionStatus.Ended || Status == AuctionStatus.Sold)
            return AuctionErrors.Auction.CannotCancelActive;

        Status = AuctionStatus.Cancelled;
        ActualEndTime = now;
        ModifiedAt = now;
        RaiseDomainEvent(new AuctionCancelledEvent(
            Id.Value.ToString(),
            reason,
            now));

        return UnitResult.Success<Error>();
    }

    private void EndAuction(AuctionStatus finalStatus, DateTime now, UserId? winnerId = null)
    {
        if (finalStatus != AuctionStatus.Ended && finalStatus != AuctionStatus.Sold)
            throw new InvalidOperationException("EndAuction requires Ended or Sold status");

        Status = finalStatus;
        ActualEndTime = now;
        ModifiedAt = now;

        if (winnerId.HasValue)
        {
            if (winnerId.Value == SellerId)
                throw new InvalidOperationException("Seller cannot be winner");

            CurrentWinnerId = winnerId;
        }

        RaiseDomainEvent(new AuctionEndedEvent(
            Id.Value.ToString(),
            finalStatus.ToString(),
            CurrentWinnerId?.Value.ToString(),
            now));
    }

    public void IncrementView()
    {
        ViewCount++;
        ModifiedAt = DateTime.UtcNow;
    }

    public void ToggleFeature(bool isFeatured, DateTime now)
    {
        IsFeatured = isFeatured;
        ModifiedAt = now;
        RaiseDomainEvent(new AuctionFeatureToggledEvent(
            Id.Value.ToString(),
            isFeatured,
            now));
    }
}