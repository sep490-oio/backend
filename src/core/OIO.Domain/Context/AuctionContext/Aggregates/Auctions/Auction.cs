using System.Net;
using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class Auction : AggregateRoot<AuctionId>, IAuditableEntity
{
    private readonly List<Bid> _bids = [];
    // private readonly List<AuctionDeposit> _deposits = [];
    private readonly List<AutoBid> _autoBids = [];
    private readonly List<AuctionWatcher> _watchers = [];
    private readonly List<AuctionPriceHistory> _priceHistories = [];
    
    public ItemId ItemId { get; private set; }
    public UserId SellerId { get; private set; }
    public Money StartingPrice { get; private set; }
    public Money? ReservePrice { get; private set; }
    public Money? BuyNowPrice { get; private set; }
    public Money CurrentPrice { get; private set; }
    public Money BidIncrement { get; private set; }
    public AuctionDuration Duration { get; private set; }
    public DateTime? ActualEndTime { get; private set; }

    public AuctionStatus Status { get; private set; }
    public UserId? CurrentWinnerId { get; private set; }

    public bool AutoExtend { get; private set; }
    public int ExtensionMinutes { get; private set; }
    public bool IsFeatured { get; private set; }

    public int ViewCount { get; private set; }
    public int BidCount { get; private set; }
    public int WatchCount { get; private set; }
    public int ExtensionCount { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    public Item Item { get; private set; } = null!;
    public IReadOnlyCollection<Bid> Bids => _bids.AsReadOnly();
    // public IReadOnlyCollection<AuctionDeposit> Deposits => _deposits.AsReadOnly();
    public IReadOnlyCollection<AutoBid> AutoBids => _autoBids.AsReadOnly();
    public IReadOnlyCollection<AuctionWatcher> Watchers => _watchers.AsReadOnly();
    public IReadOnlyCollection<AuctionPriceHistory> PriceHistories => _priceHistories.AsReadOnly();

    private Auction() { }

    private Auction(
        AuctionId id,
        ItemId itemId,
        UserId sellerId,
        DateTime nowUtc,
        AuctionDuration duration,
        Money startingPrice,
        Money bidIncrement,
        Money? reservePrice = null,
        Money? buyNowPrice = null,
        bool autoExtend = true,
        int extensionMinutes = 5)
        : base(id)
    {
        ItemId = itemId;
        SellerId = sellerId;
        StartingPrice = startingPrice;
        ReservePrice = reservePrice;
        BuyNowPrice = buyNowPrice;
        CurrentPrice = startingPrice;
        BidIncrement = bidIncrement;
        Duration = duration;
        Status = AuctionStatus.Draft;
        AutoExtend = autoExtend;
        ExtensionMinutes = extensionMinutes;
        IsFeatured = false;
        ViewCount = 0;
        BidCount = 0;
        WatchCount = 0;
        ExtensionCount = 0;
        CreatedAt = nowUtc;
    }

    public static Result<Auction, Error> Create(
        ItemId itemId,
        UserId sellerId,
        DateTime nowUtc,
        AuctionDuration duration,
        Money startingPrice,
        Money bidIncrement,
        Money? reservePrice = null,
        Money? buyNowPrice = null,
        bool autoExtend = true,
        int extensionMinutes = 5)
    {
        var check = Auction.Check(isInvariant: true);

        check
            .Field(reservePrice, x => x.ReservePrice)
            .WhenHasValue(x => x.GreaterThanOrEqual(startingPrice))
            .Field(buyNowPrice, x => x.BuyNowPrice)
            .WhenHasValue(x => x.GreaterThan(startingPrice))
            .Field(extensionMinutes, x => x.ExtensionMinutes)
            .NonNegative();
        
        var resultCheck = check.ToUnitResult();
        
        if (resultCheck.IsFailure)
        {
            return resultCheck.Error;
        }

        var auction = new Auction(
            AuctionId.From(Guid.CreateVersion7()),
            itemId,
            sellerId,
            nowUtc,
            duration,
            startingPrice,
            bidIncrement, 
            reservePrice,
            buyNowPrice,
            autoExtend,
            extensionMinutes);

        auction._priceHistories.Add(
            AuctionPriceHistory.Create(auction.Id, startingPrice, nowUtc));
        
        auction.RaiseDomainEvent(new AuctionCreatedEvent(
            $"{auction.Id}", 
            $"{itemId}",
            $"{sellerId}",
            startingPrice.Amount,
            duration.StartTime,
            duration.EndTime,
            nowUtc));

        return Result.Success<Auction, Error>(auction);
    }

    // ==================================================================================
    //                              LIFECYCLE
    // ==================================================================================
    public UnitResult<Error> Publish(DateTime nowUtc)
    {
        var result = EnsureCanTransition(AuctionStatus.Pending);

        if (result.IsFailure)
        {
            return result.Error;
        }
        
        Status = AuctionStatus.Pending;
        ModifiedAt = nowUtc;
        
        return UnitResult.Success<Error>();
    }
    
    public UnitResult<Error> Start(DateTime nowUtc)
    {
        var result = EnsureCanTransition(AuctionStatus.Active);

        if (result.IsFailure)
        {
            return result.Error;
        }
        
        Status = AuctionStatus.Active;
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionStartedEvent($"{Id}", nowUtc));
        
        return UnitResult.Success<Error>();
    }
    
    public UnitResult<Error> End(DateTime nowUtc)
    {
        var result = EnsureCanTransition(AuctionStatus.Ended);

        if (result.IsFailure)
        {
            return result.Error;
        }

        Status = AuctionStatus.Ended;
        ActualEndTime = nowUtc;

        var winningBid = GetCurrentWinningBid();

        if (winningBid is not null)
        {
            winningBid.MarkAsWon();
            CurrentWinnerId = winningBid.BidderId;

            // Mark all other active/outbid bids as cancelled
            foreach (var bid in _bids.Where(b =>
                         b.Id != winningBid.Id &&
                         (b.Status == BidStatus.Active || b.Status == BidStatus.Outbid)))
            {
                bid.Cancel();
            }

            // Mark winner's auto-bid as won
            var winnerAutoBid = _autoBids
                .FirstOrDefault(ab => ab.BidderId == winningBid.BidderId &&
                                      ab.Status == AutoBidStatus.Active);
            winnerAutoBid?.MarkAsWon(nowUtc);

            // Mark other auto-bids as outbid
            foreach (var ab in _autoBids.Where(ab =>
                         ab.BidderId != winningBid.BidderId &&
                         ab.Status == AutoBidStatus.Active))
            {
                ab.MarkAsOutbid(nowUtc);
            }
        }

        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionEndedEvent(
            $"{Id}", 
            $"{CurrentWinnerId}", 
            CurrentPrice.Amount,
            BidCount,
            IsReserveMet,
            nowUtc));
        
        return UnitResult.Success<Error>();
    }
    
     /// <summary>
    /// Called after End() to determine final outcome.
    /// Returns the resolution: Sold, Failed (no bids), Failed (reserve not met).
    /// </summary>
    public UnitResult<Error> Resolve(DateTime nowUtc)
    {
        if (Status != AuctionStatus.Ended)
            return AuctionErrors.Auction.InvalidState(Status.Id, "resolve");

        // Case 1: No bids at all
        if (BidCount == 0)
        {
            var failResult = MarkAsFailed(nowUtc);
            if (failResult.IsFailure) return failResult;

            RaiseDomainEvent(new AuctionFailedEvent(
                AuctionId: $"{Id}",
                SellerId: $"{SellerId}",
                Reason: "No bids received",
                FinalPrice: CurrentPrice.Amount,
                Currency: CurrentPrice.Currency.Id,
                TotalBids: 0,
                OccurredAt: nowUtc));

            return UnitResult.Success<Error>();
        }

        // Case 2: Has bids but reserve not met
        if (!IsReserveMet)
        {
            var failResult = MarkAsFailed(nowUtc);
            if (failResult.IsFailure) return failResult;

            RaiseDomainEvent(new AuctionFailedEvent(
                AuctionId: $"{Id}",
                SellerId: $"{SellerId}",
                Reason: "Reserve price not met",
                FinalPrice: CurrentPrice.Amount,
                Currency: CurrentPrice.Currency.Id,
                TotalBids: BidCount,
                OccurredAt: nowUtc));

            return UnitResult.Success<Error>();
        }

        // Case 3: Has winner + reserve met → Sold
        var soldResult = MarkAsSold(nowUtc);
        
        if (soldResult.IsFailure) 
            return soldResult;

        RaiseDomainEvent(new AuctionSoldEvent(
            AuctionId: $"{Id}",
            WinnerId: $"{CurrentWinnerId}",
            SellerId: $"{SellerId}",
            FinalPrice: CurrentPrice.Amount,
            Currency: CurrentPrice.Currency.Id,
            TotalBids: BidCount,
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }
     
    /// <summary>
    /// Get the runner-up bidder (second highest unique bidder).
    /// Used when winner doesn't pay.
    /// </summary>
    public Bid? GetRunnerUpBid()
    {
        if (CurrentWinnerId is null) return null;

        return _bids
            .Where(b => b.BidderId != CurrentWinnerId &&
                        (b.Status == BidStatus.Outbid || b.Status == BidStatus.Cancelled))
            .OrderByDescending(b => b.Amount.Amount)
            .FirstOrDefault();
    }
    
    /// <summary>
    /// Transfer win to runner-up (when original winner doesn't pay).
    /// </summary>
    public UnitResult<Error> TransferToRunnerUp(DateTime nowUtc)
    {
        if (Status != AuctionStatus.Sold && Status != AuctionStatus.Ended)
            return AuctionErrors.Auction.InvalidState(Status.Id, "transfer to runner-up");

        var runnerUp = GetRunnerUpBid();

        if (runnerUp is null)
            return AuctionErrors.Auction.NoRunnerUp;

        // Mark old winner's bid as cancelled
        var oldWinnerBid = GetCurrentWinningBid();
        oldWinnerBid?.Cancel();

        // Mark runner-up as new winner
        CurrentWinnerId = runnerUp.BidderId;
        CurrentPrice = runnerUp.Amount;
        runnerUp.MarkAsWon();
        ModifiedAt = nowUtc;

        // Re-mark as Sold
        Status = AuctionStatus.Sold;

        RaiseDomainEvent(new AuctionSoldEvent(
            AuctionId: $"{Id}",
            WinnerId: $"{CurrentWinnerId}",
            SellerId: $"{SellerId}",
            FinalPrice: CurrentPrice.Amount,
            Currency: CurrentPrice.Currency.Id,
            TotalBids: BidCount,
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }
    
    public UnitResult<Error> MarkAsSold(DateTime nowUtc)
    {
        var result = EnsureCanTransition(AuctionStatus.Sold);

        if (result.IsFailure)
        {
            return result.Error;
        }

        if (CurrentWinnerId is null)
            return AuctionErrors.Auction.InvalidState(Status.Id, "mark as sold without a winner");

        if (!IsReserveMet)
            return AuctionErrors.Auction.InvalidState(Status.Id, "mark as sold when reserve price not met");

        Status = AuctionStatus.Sold;
        ModifiedAt = nowUtc;

        return result;
    }
    
    public UnitResult<Error> MarkAsFailed(DateTime nowUtc)
    {
        var result = EnsureCanTransition(AuctionStatus.Failed);

        if (result.IsFailure)
        {
            return result.Error;
        }
        
        Status = AuctionStatus.Failed;
        ModifiedAt = nowUtc;

        return result;
    }

    public UnitResult<Error> CancelAuction(string reason, DateTime nowUtc)
    {
        if (!Status.CanTransitionTo(AuctionStatus.Cancelled))
            return AuctionErrors.Auction.InvalidState(Status.Id, "cancel");

        Status = AuctionStatus.Cancelled;
        ActualEndTime = nowUtc;
        ModifiedAt = nowUtc;
        
        foreach (var bid in _bids.Where(b =>
                     b.Status == BidStatus.Active || b.Status == BidStatus.Winning))
        {
            bid.Cancel();
        }

        // Mark all auto-bids as outbid
        foreach (var ab in _autoBids.Where(ab => ab.Status == AutoBidStatus.Active))
        {
            ab.MarkAsOutbid(nowUtc);
        }
        
        RaiseDomainEvent(new AuctionCancelledEvent(
            Id.Value.ToString(),
            reason,
            nowUtc));

        return UnitResult.Success<Error>();
    }
    
    // ==================================================================================
    //                              MANUAL BIDDING
    // ==================================================================================
    public Result<Bid, Error> PlaceBid(
        UserId bidderId,
        Money amount, 
        DateTime nowUtc,
        TimeSpan extensionThresholdMinutes, 
        int maxExtensions,
        TimeSpan maxDuration,
        IPAddress? ipAddress = null)
    {
        var result = EnsureAcceptsBids(nowUtc);
        
        if (result.IsFailure)
        {
            return result.Error;
        }
        result = EnsureNotSeller(bidderId);
        
        if (result.IsFailure)
        {
            return result.Error;
        }

        var minimumBid = GetMinimumBidAmount();
        
        if (amount < minimumBid)
            return AuctionErrors.Bid.TooLow(amount, minimumBid);

        // Mark previous winning bid as outbid
        var previousWinning = GetCurrentWinningBid();
        UserId? previousBidderId = null;

        if (previousWinning is not null)
        {
            previousBidderId = previousWinning.BidderId;
            previousWinning.MarkAsOutbid();

            if (previousBidderId != bidderId)
            {
                RaiseDomainEvent(new OutbidEvent(
                    AuctionId: $"{Id}", 
                    OutbidBidderId: $"{previousBidderId}", 
                    NewHighBidderId: $"{bidderId}",
                    NewHighestBid: amount.Amount,
                    OutbidAmount: CurrentPrice.Amount,
                    OccurredAt: nowUtc));
            }
        }

        // Create and add bid
        var bid = Bid.Create(
            auctionId: Id, 
            bidderId: bidderId,
            amount: amount,
            autoBidId: null,
            ipAddress: ipAddress,
            nowUtc: nowUtc);
        
        bid.MarkAsWinning();
        
        _bids.Add(bid);

        // Update auction state
        UpdatePriceAndCount(amount, bid.Id, nowUtc);

        // Auto-extend check
        result = TryAutoExtend(
            nowUtc: nowUtc,
            extensionThresholdMinutes: extensionThresholdMinutes,
            maxExtensions: maxExtensions,
            maxDuration: maxDuration,
            triggerByBidId: bid.Id);

        if (result.IsFailure)
        {
            return result.Error;
        }
        
        RaiseDomainEvent(new BidPlacedEvent(
            AuctionId: $"{Id}", 
            BidId: $"{bid.Id}",
            BidderId: $"{bidderId}",
            Amount: amount.Amount, 
            PreviousHighestBid: CurrentPrice.Amount,
            IsAutoBid: false,
            PreviousBidderId:  previousBidderId?.ToString(),
            BidCount: BidCount,
            BidTime: bid.CreatedAt,
            nowUtc));

        // Process auto-bids from OTHER bidders
        result = ProcessAutoBids(excludeBidderId: bidderId, nowUtc);

        if (result.IsFailure)
        {
            return result.Error;
        }

        return bid;
    }
    
    /// <summary>
    /// Execute Buy Now. Immediately ends the auction with the buyer as winner.
    /// </summary>
    public Result<Bid, Error> ExecuteBuyNow(
        UserId buyerId,
        DateTime nowUtc,
        IPAddress? ipAddress = null)
    {
        var result = EnsureAcceptsBids(nowUtc);
        
        if (result.IsFailure)
        {
            return result.Error;
        }
        result = EnsureNotSeller(buyerId);
        
        if (result.IsFailure)
        {
            return result.Error;
        }

        if (BuyNowPrice is null)
            return AuctionErrors.Auction.NotSupportBuyNow;

        // Cancel all existing active/winning bids
        foreach (var existingBid in _bids.Where(b =>
                     b.Status == BidStatus.Active || b.Status == BidStatus.Winning))
        {
            existingBid.Cancel();
        }

        // Cancel all auto-bids
        foreach (var ab in _autoBids.Where(ab => ab.Status == AutoBidStatus.Active))
        {
            ab.MarkAsOutbid(nowUtc);
        }

        // Place winning bid at buy now price
        var bid = Bid.Create(Id, buyerId, BuyNowPrice, autoBidId: null, ipAddress, nowUtc);
        bid.MarkAsWon();
        _bids.Add(bid);

        // Update auction state
        CurrentPrice = BuyNowPrice;
        CurrentWinnerId = buyerId;
        BidCount++;
        ActualEndTime = nowUtc;
        Status = AuctionStatus.Sold;
        ModifiedAt = nowUtc;

        _priceHistories.Add(AuctionPriceHistory.Create(Id, BuyNowPrice,nowUtc, bid.Id));

        RaiseDomainEvent(new BuyNowExecutedEvent(
            $"{Id}",
            $"{buyerId}", 
            BuyNowPrice.Amount,
            nowUtc));

        return bid;
    }
    
    // ==================================================================================
    //                              AUTO BIDDING
    // ==================================================================================
    public Result<AutoBid, Error> ConfigureAutoBid(
        UserId bidderId,
        Money maxAmount,
        DateTime nowUtc,
        Money? incrementAmount = null)
    {
        var result = EnsureAcceptsBids(nowUtc);
        
        if (result.IsFailure)
        {
            return result.Error;
        }
        result = EnsureNotSeller(bidderId);
        
        if (result.IsFailure)
        {
            return result.Error;
        }

        var check = AutoBid.Check(isInvariant: true)
            .Field(maxAmount, x => x.MaxAmount)
            .GreaterThanOrEqual(CurrentPrice)
            .Field(incrementAmount, x => x.IncrementAmount)
            .WhenHasValue(x => x.GreaterThan(Money.Zero(maxAmount.Currency)));
        
        var validationResult = check.ToResult();

        if (validationResult.IsFailure)
        {
            return validationResult.Error;
        }
        
        // Check existing auto-bid (UNIQUE constraint: auction_id + bidder_id)
        var existing = _autoBids.FirstOrDefault(ab => ab.BidderId == bidderId);

        if (existing is not null)
        {
            var resultUpdateExiting = existing.UpdateConfig(maxAmount, incrementAmount, nowUtc);
            
            if (resultUpdateExiting.IsFailure)
            {
                return resultUpdateExiting.Error;
            }
            
            return existing;
        }

        var autoBid = AutoBid.Create(
            Id,
            bidderId,
            maxAmount,
            CurrentPrice,
            incrementAmount,
            nowUtc);
        
        _autoBids.Add(autoBid);

        RaiseDomainEvent(new AuctionAutoBidConfiguredEvent(
            $"{Id}", 
            $"{bidderId}",
            maxAmount.Amount,
            nowUtc));
        
        var currentWinning = GetCurrentWinningBid();
        
        if (currentWinning is not null && currentWinning.BidderId != bidderId)
        {
            result = ProcessSingleAutoBid(autoBid, nowUtc);
            
            if (result.IsFailure)
            {
                return result.Error;
            }
        }

        return autoBid;
    }

    
    public UnitResult<Error>  PauseAutoBid(
        UserId bidderId,
        DateTime nowUtc)
    {
        var (_, isFailure, autoBid, error) = FindAutoBid(bidderId);

        if (isFailure)
        {
            return error;
        }
        
        var result = autoBid.Pause(nowUtc);
        
        return result.IsFailure ? error : result;
    }
    
    public UnitResult<Error> ResumeAutoBid(
        UserId bidderId,
        DateTime nowUtc)
    {
        var result = EnsureAcceptsBids(nowUtc);
        
        if (result.IsFailure)
        {
            return result.Error;
        }
        
        var (_, isFailure, autoBid, error) = FindAutoBid(bidderId);

        if (isFailure)
        {
            return error;
        }
        
        autoBid.Resume(nowUtc);

        // Immediately try to bid if there's a winning bid from someone else
        var currentWinning = GetCurrentWinningBid();
        if (currentWinning is null || currentWinning.BidderId == bidderId) 
            return result;
        
        (_,isFailure,_, error) = ProcessSingleAutoBid(autoBid, nowUtc);
        
        return isFailure ? error : result;
    }
    
    private UnitResult<Error> ProcessAutoBids(
        UserId excludeBidderId,
        DateTime nowUtc)
    {
        // Get eligible auto-bids, ordered by max amount DESC, then by creation time ASC
        var eligibleAutoBids = _autoBids
            .Where(ab => ab.BidderId != excludeBidderId &&
                         ab.IsEnabled &&
                         ab.Status == AutoBidStatus.Active)
            .OrderByDescending(ab => ab.MaxAmount.Amount)
            .ThenBy(ab => ab.CreatedAt)
            .ToList();

        if (eligibleAutoBids.Count == 0) 
            return UnitResult.Success<Error>();

        // Try the highest auto-bidder first
        foreach (var autoBid in eligibleAutoBids)
        {
            var processResult = ProcessSingleAutoBid(autoBid, nowUtc);
            
            if (processResult.IsFailure)
            {
                return processResult.Error;
            }
            if (processResult.Value)
            {
                // After one auto-bid succeeds, check if the ORIGINAL bidder
                // also has an auto-bid that should respond
                var originalBidderAutoBid = _autoBids
                    .FirstOrDefault(ab => ab.BidderId == excludeBidderId &&
                                          ab.IsEnabled &&
                                          ab.Status == AutoBidStatus.Active);

                if (originalBidderAutoBid is not null)
                {
                    // Recursive auto-bid battle between two auto-bidders
                    var processAutoResult = ProcessAutoBidBattle(autoBid, originalBidderAutoBid, nowUtc);
                    
                    if (processAutoResult.IsFailure)
                    {
                        return processAutoResult.Error;
                    }
                }

                break;
            }

            // This auto-bid can't compete, mark as outbid
            autoBid.MarkAsOutbid(nowUtc);
        }
        
        return UnitResult.Success<Error>();
    }
    
    private Result<bool, Error> ProcessSingleAutoBid(
        AutoBid autoBid,
        DateTime nowUtc)
    {
        var minimumRequired = GetMinimumBidAmount();

        if (!autoBid.CanBid(minimumRequired))
            return false;

        var (_, isFailure, bidAmount, error) = autoBid
            .CalculateNextBidAmount(minimumRequired);
            
        if (isFailure)
        {
            return error;
        }

        // Place the auto-bid
        PlaceAutoBidInternal(autoBid, bidAmount, nowUtc);

        return true;
    }
    
    private UnitResult<Error> ProcessAutoBidBattle(
        AutoBid autoBidA,
        AutoBid autoBidB,
        DateTime nowUtc)
    {
        const int maxRounds = 100; // Safety: prevent infinite loops
        var round = 0;

        var currentAttacker = autoBidB; // B responds to A's bid
        var currentDefender = autoBidA;

        while (round < maxRounds)
        {
            round++;
            var minimumRequired = GetMinimumBidAmount();

            if (!currentAttacker.CanBid(minimumRequired))
            {
                currentAttacker.MarkAsOutbid(nowUtc);
                break;
            }

            var (_, isFailure, bidAmount, error) = currentAttacker
                .CalculateNextBidAmount(minimumRequired);
            
            if (isFailure)
            {
                return error;
            }
            
            PlaceAutoBidInternal(currentAttacker, bidAmount, nowUtc);

            // Swap roles
            (currentAttacker, currentDefender) = (currentDefender, currentAttacker);

            // Check if defender can still respond
            var nextMinimum = GetMinimumBidAmount();
            
            if (!currentAttacker.CanBid(nextMinimum))
            {
                currentAttacker.MarkAsOutbid(nowUtc);
                break;
            }
        }
        
        return UnitResult.Success<Error>();
    }
    
    private void PlaceAutoBidInternal(
        AutoBid autoBid,
        Money bidAmount,
        DateTime nowUtc)
    {
        // Mark previous winning as outbid
        var previousWinning = GetCurrentWinningBid();
        var previousBidderId = previousWinning?.BidderId;
        previousWinning?.MarkAsOutbid();

        // Create bid linked to auto-bid
        var bid = Bid.Create(
            Id,
            autoBid.BidderId,
            bidAmount, 
            autoBid.Id,
            null,
            nowUtc);
        
        bid.MarkAsWinning();
        
        _bids.Add(bid);
        
        autoBid.UpdateCurrentAmount(bidAmount, nowUtc);

        // Update auction state
        UpdatePriceAndCount(bidAmount, bid.Id, nowUtc);

        // Events
        RaiseDomainEvent(new BidPlacedEvent(
            AuctionId: $"{Id}", 
            BidId: $"{bid.Id}",
            BidderId: $"{autoBid.BidderId}",
            Amount: bidAmount.Amount, 
            PreviousHighestBid: CurrentPrice.Amount,
            IsAutoBid: true,
            PreviousBidderId:  previousBidderId?.ToString(),
            BidCount: BidCount,
            BidTime: bid.CreatedAt,
            nowUtc));

        if (previousBidderId.HasValue && previousBidderId.Value != autoBid.BidderId)
        {
            RaiseDomainEvent(new OutbidEvent(
                AuctionId: $"{Id}", 
                OutbidBidderId: $"{previousBidderId}", 
                NewHighBidderId: $"{autoBid.BidderId}",
                NewHighestBid: bidAmount.Amount,
                OutbidAmount: CurrentPrice.Amount,
                OccurredAt: nowUtc));
        }
    }
    
    // ==================================================================================
    //                              WATCHERS
    // ==================================================================================
    public Result<AuctionWatcher, Error> AddWatcher(
        UserId userId,
        DateTime nowUtc,
        bool notifyOnBid = true,
        bool notifyOnEnd = true)
    {
        if (_watchers.Any(w => w.UserId == userId))
            return AuctionErrors.Watcher.AlreadyWatching;
        
        
        if (userId == SellerId)
            return AuctionErrors.Watcher.CannotWatchOwnAuction;

        var watcher = new AuctionWatcher(
            AuctionWatcherId.From(Guid.CreateVersion7()),
            Id,
            userId,
            nowUtc,
            notifyOnBid,
            notifyOnEnd);
        
        _watchers.Add(watcher);
        WatchCount++;
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionWatcherAddedEvent(
            $"{Id}",
            $"{userId}",
            nowUtc));

        return watcher;
    }
    
    public void RemoveWatcher(UserId userId, DateTime nowUtc)
    {
        var watcher = _watchers.FirstOrDefault(w => w.UserId == userId);
        if (watcher is null)
            return;

        _watchers.Remove(watcher);
        WatchCount = Math.Max(0, WatchCount - 1);
        ModifiedAt = nowUtc;
    }
    
    public UnitResult<Error> UpdateWatcherPreferences(
        UserId userId,
        DateTime nowUtc,
        bool? notifyOnBid,
        bool? notifyOnEnd)
    {
        var watcher = _watchers.FirstOrDefault(w => w.UserId == userId);

        if (watcher is null)
        {
            return AuctionErrors.Watcher.NotFound(Id, userId);
        }

        if (userId == SellerId)
        {
            return AuctionErrors.Watcher.CannotWatchOwnAuction;
        }

        watcher.UpdateNotificationSettings(notifyOnBid, notifyOnEnd);
        ModifiedAt = nowUtc;
        
        return UnitResult.Success<Error>();
    }
    
    // ==================================================================================
    //                              COUNTERS & SETTINGS
    // ==================================================================================

    public void IncrementView(DateTime nowUtc)
    {
        ViewCount++;
        ModifiedAt = nowUtc;
    }

    public void ToggleFeature(bool isFeatured, DateTime now)
    {
        IsFeatured = isFeatured;
        ModifiedAt = now;
        RaiseDomainEvent(new AuctionFeatureToggledEvent(
            $"{Id}",
            isFeatured,
            now));
    }
    
    // ==================================================================================
    //                              QUERIES
    // ==================================================================================
    public Money GetMinimumBidAmount()
    {
        return BidCount == 0
            ? StartingPrice
            : CurrentPrice + BidIncrement;
    }
    
    public Bid? GetCurrentWinningBid()
    {
        return _bids
            .Where(b => b.Status == BidStatus.Winning)
            .MaxBy(b => b.Amount.Amount);
    }
    
    public bool IsReserveMet =>
        ReservePrice is null || CurrentPrice >= ReservePrice;

    public bool HasBuyNow => BuyNowPrice is not null;

    public TimeSpan RemainingTime(DateTime nowUtc) =>
        Duration.RemainingTime(nowUtc);

    public bool IsEndingSoon(DateTime nowUtc, TimeSpan extensionThresholdMinutes) =>
        Status == AuctionStatus.Active &&
        RemainingTime(nowUtc) <= extensionThresholdMinutes;
    
    public IReadOnlyList<Bid> GetBidsByBidder(Guid bidderId) =>
        _bids.Where(b => b.BidderId == bidderId).ToList().AsReadOnly();
    
    public AutoBid? GetAutoBidForBidder(Guid bidderId) =>
        _autoBids.FirstOrDefault(ab => ab.BidderId == bidderId);
    public bool IsWatchedBy(Guid userId) =>
        _watchers.Any(w => w.UserId == userId);
    
    // ==================================================================================
    //                              PRIVATE HELPERS
    // ==================================================================================
    private void UpdatePriceAndCount(
        Money newPrice,
        BidId bidId,
        DateTime nowUtc)
    {
        CurrentPrice = newPrice;
        BidCount++;
        ModifiedAt = nowUtc;
        _priceHistories.Add(AuctionPriceHistory.Create(Id, newPrice, nowUtc, bidId));
    }
    
    private UnitResult<Error> TryAutoExtend(
        DateTime nowUtc,
        TimeSpan extensionThresholdMinutes, 
        int maxExtensions,
        TimeSpan maxDuration,
        BidId triggerByBidId)
    {
        if (!AutoExtend || ExtensionCount >= maxExtensions || !IsEndingSoon(nowUtc, extensionThresholdMinutes)) 
            return UnitResult.Success<Error>();

        var oldEndTime = Duration.EndTime;
        var extension = TimeSpan.FromMinutes(ExtensionMinutes);
        
        var durationResult = Duration.Extend(extension, maxDuration);
        if (durationResult.IsFailure)
        {
            return durationResult.Error;
        }

        Duration = durationResult.Value ;
        ExtensionCount++;
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionExtendedEvent(
            AuctionId: $"{Id}",
            TriggerByBidId: $"{triggerByBidId}",
            PreviousEndTime: oldEndTime, 
            NewEndTime: Duration.EndTime,
            ExtensionMinutes: ExtensionMinutes,
            ExtensionCount: ExtensionCount,
            OccurredAt: nowUtc));
        
        return UnitResult.Success<Error>();
    }
    
    private UnitResult<Error>  EnsureAcceptsBids(DateTime nowUtc)
    {
        if (!Status.AcceptsBids)
            return AuctionErrors.Auction.InvalidState(Status.Id, "place bid");

        if (Duration.HasEnded(nowUtc))
            return AuctionErrors.Auction.Expired;
        
        return UnitResult.Success<Error>();
    }
    
    private UnitResult<Error> EnsureNotSeller(UserId bidderId)
    {
        return bidderId == SellerId ? 
            AuctionErrors.Auction.SelfBid :
            UnitResult.Success<Error>();
    }
    
    private UnitResult<Error> EnsureCanTransition(AuctionStatus target)
    {
        return !Status.CanTransitionTo(target) ?
            AuctionErrors.Auction.InvalidState(Status.Id, $"transition to {target.Id}") :
            UnitResult.Success<Error>();
    }

    private Result<AutoBid, Error> FindAutoBid(UserId bidderId)
    {
        var autobid = _autoBids.FirstOrDefault(ab => ab.BidderId == bidderId);

        if (autobid is null)
        {
            return AuctionErrors.AutoBid.NotFoundForBidder(bidderId);
        }
        
        return autobid;
    }
}