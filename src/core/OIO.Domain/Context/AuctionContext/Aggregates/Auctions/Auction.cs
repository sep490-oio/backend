using System.Net;
using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class Auction : AggregateRoot<AuctionId>, IAuditableEntity
{
    private readonly List<Bid> _bids = [];
    private readonly List<AutoBid> _autoBids = [];
    private readonly List<SealedBid> _sealedBids = [];
    private readonly List<AuctionWatcher> _watchers = [];
    private readonly List<AuctionPriceHistory> _priceHistories = [];
    private readonly List<AuctionDeposit> _deposits = [];
    private readonly List<AuctionParticipant> _participants = [];
    private readonly List<AuctionWinnerOffer> _winnerOffers = [];
    private readonly List<AuctionRelistHistory> _relistHistories = [];
    private readonly List<AuctionEmergency> _emergencies = [];
    private readonly List<AuctionBuyNowReservation> _buyNowReservations = [];
    
    public ItemId ItemId { get; private set; }
    public AuctionType? AuctionType { get; private set; }
    
    public AuctionPricing Pricing { get; private set; }
    public AuctionInfo? Info { get; private set; }

    // ── Result ──
    public DateTime? ActualEndTime { get; private set; }
    public AuctionStatus Status { get; private set; }
    public UserId? WinnerId { get; private set; }

    // ── VO: PriorityInfo ──
    public PriorityInfo? Priority { get; private set; }

    // ── Admin assignment ──
    public UserId? AssignedAdminId { get; private set; }
    public DateTime? AssignedAt { get; private set; }

    // ── Moderation ──
    public bool VerifyByPlatform { get; private set; }
    public int RejectionCount { get; private set; }

    // ── Counters/Flags ──
    public bool IsFeatured { get; private set; }
    public int ViewCount { get; private set; }
    public int BidCount { get; private set; }
    public int WatchCount { get; private set; }

    // ── Audit ──
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    
    // Navigation — child entities
    public Item Item { get; private set; } = null!;
    public IReadOnlyCollection<Bid> Bids => _bids.AsReadOnly();
    public IReadOnlyCollection<AutoBid> AutoBids => _autoBids.AsReadOnly();
    public IReadOnlyCollection<AuctionWatcher> Watchers => _watchers.AsReadOnly();
    public IReadOnlyCollection<AuctionPriceHistory> PriceHistories => _priceHistories.AsReadOnly();
    public IReadOnlyCollection<AuctionDeposit> Deposits => _deposits.AsReadOnly();
    public IReadOnlyCollection<AuctionParticipant> Participants => _participants.AsReadOnly();
    public IReadOnlyCollection<SealedBid> SealedBids => _sealedBids.AsReadOnly();
    public IReadOnlyCollection<AuctionWinnerOffer> WinnerOffers => _winnerOffers.AsReadOnly();
    public IReadOnlyCollection<AuctionRelistHistory> RelistHistories => _relistHistories.AsReadOnly();
    public IReadOnlyCollection<AuctionEmergency> Emergencies => _emergencies.AsReadOnly();
    public IReadOnlyCollection<AuctionBuyNowReservation> BuyNowReservations => _buyNowReservations.AsReadOnly();

    private Auction() { }

    private Auction(
        AuctionId id,
        ItemId itemId,
        AuctionType auctionType,
        AuctionPricing pricing,
        AuctionInfo? info,
        DateTime nowUtc)
        : base(id)
    {
        ItemId = itemId;
        AuctionType = auctionType;
        Pricing = pricing;
        Info = info;
        Priority = PriorityInfo.Default;
        Status = AuctionStatus.Draft;
        IsFeatured = false;
        ViewCount = 0;
        BidCount = 0;
        WatchCount = 0;
        CreatedAt = nowUtc;
    }

    public static Result<Auction, Error> Create(
        UserId sellerId,
        ItemId itemId,
        AuctionType auctionType,
        AuctionPricing pricing,
        DateTime nowUtc,
        AuctionInfo? info = null)
    {
        var auction = new Auction(
            id: AuctionId.From(Guid.CreateVersion7()),
            itemId: itemId,
            auctionType: auctionType,
            pricing: pricing,
            info: info,
            nowUtc: nowUtc);

        auction._priceHistories.Add(
            AuctionPriceHistory.CreateStartingPrice(
                auctionId: auction.Id,
                price: pricing.StartingPrice,
                recordedAt: nowUtc));

        auction.RaiseDomainEvent(new AuctionCreatedEvent(
            AuctionId: $"{auction.Id}",
            ItemId: $"{itemId}",
            SellerId: $"{sellerId}",
            StartingPrice: pricing.StartingAmount,
            StartTime: info?.StartTime ?? DateTime.MinValue,
            EndTime: info?.EndTime ?? DateTime.MinValue,
            OccurredAt: nowUtc));

        return Result.Success<Auction, Error>(auction);
    }

    // ==================================================================================
    //                              LIFECYCLE
    // ==================================================================================
    public UnitResult<Error> SubmitConfiguration(DateTime nowUtc)
    {
        if (Status != AuctionStatus.Draft)
            return AuctionErrors.Auction.CannotSubmit;

        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionSubmittedEvent(
            AuctionId: $"{Id}",
            ItemId: $"{ItemId}",
            SellerId: $"{Item.SellerId}",
            VerifyByPlatform: VerifyByPlatform,
            OccurredAt: nowUtc));

        if (Info is null)
        {
            Status = AuctionStatus.Approved;
            return UnitResult.Success<Error>();
        }

        Status = AuctionStatus.Scheduled;

        RaiseDomainEvent(new AuctionScheduledEvent(
            AuctionId: $"{Id}",
            StartTime: Info.StartTime,
            EndTime: Info.EndTime,
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkApproved(DateTime nowUtc)
    {
        var result = EnsureCanTransition(AuctionStatus.Approved);

        if (result.IsFailure)
            return result.Error;

        Status = AuctionStatus.Approved;
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionApprovedEvent(
            AuctionId: $"{Id}",
            ItemId: $"{ItemId}",
            ReviewerId: $"{Item.ReviewedBy}",
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkRejected(string reason, DateTime nowUtc)
    {
        // Bug #1 fix: relax guard so admin can reject an auction at any pre-bidding stage.
        // Previous code accepted only Pending — but SubmitConfiguration bypasses Pending and
        // jumps to Approved/Scheduled directly, making MarkRejected unreachable.
        // Active/Ended/Sold/Terminated/etc. cannot be soft-rejected (use Terminate instead).
        if (Status != AuctionStatus.Pending &&
            Status != AuctionStatus.Approved &&
            Status != AuctionStatus.Scheduled)
            return AuctionErrors.Auction.InvalidState(Status.Id, "reject");

        RejectionCount++;
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionRejectedEvent(
            AuctionId: $"{Id}",
            ItemId: $"{ItemId}",
            ReviewerId: $"{Item.ReviewedBy}",
            Reason: reason,
            RejectionCount: RejectionCount,
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> SetTiming(AuctionInfo timing, DateTime nowUtc)
    {
        if (Status != AuctionStatus.Approved)
            return AuctionErrors.Auction.CannotSetTiming;

        if (!timing.HasQualification)
            return AuctionErrors.Auction.QualificationWindowRequired;

        var result = EnsureCanTransition(AuctionStatus.Scheduled);

        if (result.IsFailure)
            return result.Error;

        Info = timing;
        Status = AuctionStatus.Scheduled;
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionScheduledEvent(
            AuctionId: $"{Id}",
            StartTime: timing.StartTime,
            EndTime: timing.EndTime,
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> UpdateConfiguration(
        AuctionType auctionType,
        AuctionPricing pricing,
        AuctionInfo? info,
        DateTime nowUtc)
    {
        if (Status == AuctionStatus.Active ||
            Status == AuctionStatus.Ended ||
            Status == AuctionStatus.Sold ||
            Status == AuctionStatus.PaymentDefaulted ||
            Status == AuctionStatus.Failed ||
            Status == AuctionStatus.Cancelled ||
            Status == AuctionStatus.Terminated)
        {
            return AuctionErrors.Auction.CannotEdit;
        }

        if (BidCount > 0)
            return AuctionErrors.Auction.CannotEdit;

        if (Status == AuctionStatus.Scheduled && info is null)
            return AuctionErrors.Auction.TimingRequired;

        AuctionType = auctionType;
        Pricing = pricing;
        Info = info;

        if (Status == AuctionStatus.Approved && info is not null)
        {
            if (!info.HasQualification)
                return AuctionErrors.Auction.QualificationWindowRequired;
            Status = AuctionStatus.Scheduled;
        }

        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> ApplyCuration(
        UserId? assignedAdminId,
        PriorityInfo? priority,
        bool? isFeatured,
        DateTime nowUtc)
    {
        if (Status == AuctionStatus.Failed ||
            Status == AuctionStatus.Sold ||
            Status == AuctionStatus.PaymentDefaulted ||
            Status == AuctionStatus.Cancelled ||
            Status == AuctionStatus.Terminated)
            return AuctionErrors.Auction.InvalidState(Status.Id, "curate");

        if (assignedAdminId != AssignedAdminId)
        {
            AssignedAdminId = assignedAdminId;
            AssignedAt = assignedAdminId.HasValue ? nowUtc : null;
        }

        if (priority is not null)
        {
            Priority = priority;
        }

        if (isFeatured.HasValue)
        {
            IsFeatured = isFeatured.Value;
        }

        ModifiedAt = nowUtc;

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Resubmit(bool verifyByPlatform, DateTime nowUtc)
    {
        if (Status != AuctionStatus.Pending)
            return AuctionErrors.Auction.InvalidState(Status.Id, "resubmit");

        VerifyByPlatform = verifyByPlatform;
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

    public bool HasBidEligibleParticipants(DateTime nowUtc)
    {
        return _participants.Count(participant => IsBidEligibleParticipant(participant, nowUtc)) >= 2;
    }
    
    /// <summary>
    /// Transition auction to Ended state. Does NOT determine winner — that happens in Resolve().
    /// </summary>
    public UnitResult<Error> End(DateTime nowUtc)
    {
        var result = EnsureCanTransition(AuctionStatus.Ended);

        if (result.IsFailure)
            return result.Error;

        Status = AuctionStatus.Ended;
        ActualEndTime = nowUtc;
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionEndedEvent(
            $"{Id}",
            $"{GetCurrentWinningBid()?.BidderId}",
            Pricing.CurrentAmount,
            BidCount,
            Pricing.ReserveMet,
            nowUtc));

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Called after End() to determine final outcome: Sold, Failed (no bids), or Failed (reserve not met).
    /// Bid/auto-bid Won/Outbid marking happens HERE — not in End() — to avoid orphaned Won state on failed auctions.
    /// </summary>
    public UnitResult<Error> Resolve(DateTime nowUtc)
    {
        if (Status != AuctionStatus.Ended)
            return AuctionErrors.Auction.InvalidState(Status.Id, "resolve");

        var winningBid = GetCurrentWinningBid();

        // Case 1: No bids at all
        if (BidCount == 0 || winningBid is null)
        {
            TerminalizeAllAutoBids(nowUtc);

            var failResult = MarkAsFailed(nowUtc);
            if (failResult.IsFailure) return failResult;

            RaiseDomainEvent(new AuctionFailedEvent(
                AuctionId: $"{Id}",
                SellerId: $"{Item.SellerId}",
                Reason: "No bids received",
                FinalPrice: Pricing.CurrentAmount,
                Currency: Pricing.Currency.Id,
                TotalBids: 0,
                OccurredAt: nowUtc));

            return UnitResult.Success<Error>();
        }

        // Case 2: Has bids but reserve not met
        if (!Pricing.ReserveMet)
        {
            TerminalizeAllAutoBids(nowUtc);

            // Cancel winning bids since auction failed
            foreach (var bid in _bids.Where(b => b.Status == BidStatus.Winning))
                bid.Cancel();

            var failResult = MarkAsFailed(nowUtc);
            if (failResult.IsFailure) return failResult;

            RaiseDomainEvent(new AuctionFailedEvent(
                AuctionId: $"{Id}",
                SellerId: $"{Item.SellerId}",
                Reason: "Reserve price not met",
                FinalPrice: Pricing.CurrentAmount,
                Currency: Pricing.Currency.Id,
                TotalBids: BidCount,
                OccurredAt: nowUtc));

            return UnitResult.Success<Error>();
        }

        // Case 3: Has winner + reserve met → Sold — NOW mark Won
        winningBid.MarkAsWon();
        WinnerId = winningBid.BidderId;

        foreach (var bid in _bids.Where(b =>
                     b.Id != winningBid.Id &&
                     (b.Status == BidStatus.Active || b.Status == BidStatus.Winning || b.Status == BidStatus.Outbid)))
        {
            bid.Cancel();
        }

        SyncAutoBidStateToWinner(winningBid.BidderId, nowUtc);

        var soldResult = MarkAsSold(nowUtc);
        if (soldResult.IsFailure)
            return soldResult;

        RaiseDomainEvent(new AuctionSoldEvent(
            AuctionId: $"{Id}",
            WinnerId: $"{WinnerId}",
            SellerId: $"{Item.SellerId}",
            FinalPrice: Pricing.CurrentAmount,
            Currency: Pricing.Currency.Id,
            TotalBids: BidCount,
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }
     
    /// <summary>
    /// Get the runner-up bidder (second-highest unique bidder).
    /// Used when winner doesn't pay.
    /// </summary>
    public Bid? GetRunnerUpBid()
    {
        if (WinnerId is null) return null;

        return GetRankedBids()
            .FirstOrDefault(b => b.BidderId != WinnerId);
    }

    public IReadOnlyList<Bid> GetRankedBids()
    {
        return _bids
            .Where(b => b.Status == BidStatus.Outbid ||
                        b.Status == BidStatus.Winning ||
                        b.Status == BidStatus.Won ||
                        b.Status == BidStatus.Cancelled)
            .GroupBy(b => b.BidderId)
            .Select(group => group
                .OrderByDescending(b => b.Amount.Amount)
                .ThenBy(b => b.CreatedAt)
                .First())
            .OrderByDescending(b => b.Amount.Amount)
            .ThenBy(b => b.CreatedAt)
            .ToList();
    }
    
    /// <summary>
    /// Transfer win to runner-up (when original winner doesn't pay).
    /// </summary>
    public UnitResult<Error> TransferToRunnerUp(DateTime nowUtc)
    {
        if (Status != AuctionStatus.Sold &&
            Status != AuctionStatus.Ended &&
            Status != AuctionStatus.PaymentDefaulted)
            return AuctionErrors.Auction.InvalidState(Status.Id, "transfer to runner-up");

        var runnerUp = GetRunnerUpBid();

        if (runnerUp is null)
            return AuctionErrors.Auction.NoRunnerUp;

        // Bug #9 fix: capture the previous winner BEFORE overwriting WinnerId so we can
        // emit AuctionWinnerTransferredEvent for the order-cancellation handler.
        var previousWinnerId = WinnerId;

        CancelWinnerBidsExcept(runnerUp);

        var newPrice = Pricing.WithNewBid(runnerUp.Amount.Amount, Pricing.StartingAmount);

        if (newPrice.IsFailure)
        {
            return newPrice.Error;
        }

        // Mark runner-up as new winner
        WinnerId = runnerUp.BidderId;
        Pricing = newPrice.Value;
        runnerUp.MarkAsWon();
        SyncAutoBidStateToWinner(runnerUp.BidderId, nowUtc);
        BidCount = _bids.Count(b => b.Status != Enums.BidStatus.Cancelled);
        ModifiedAt = nowUtc;

        // Bug #9 fix: validate the transition through the state-machine map instead of
        // overwriting Status directly. PaymentDefaulted→Sold and Ended→Sold are allowed;
        // Sold→Sold is not, but the upstream guard above already excludes that case if the
        // map is well-formed. EnsureCanTransition gives us defense-in-depth.
        var transitionResult = EnsureCanTransition(AuctionStatus.Sold);
        if (transitionResult.IsFailure)
            return transitionResult.Error;

        Status = AuctionStatus.Sold;

        // Bug #9 fix: emit transfer event BEFORE AuctionSoldEvent so the order-cancellation
        // handler can close the previous winner's PendingPayment order before the new one
        // is created by AuctionSoldEventHandler. Without this, two orders would coexist:
        // the defaulted winner's stale PendingPayment + the runner-up's new PendingPayment.
        if (previousWinnerId is not null)
        {
            RaiseDomainEvent(new AuctionWinnerTransferredEvent(
                AuctionId: $"{Id}",
                PreviousWinnerId: $"{previousWinnerId}",
                NewWinnerId: $"{WinnerId}",
                OccurredAt: nowUtc));
        }

        RaiseDomainEvent(new AuctionSoldEvent(
            AuctionId: $"{Id}",
            WinnerId: $"{WinnerId}",
            SellerId: $"{Item.SellerId}",
            FinalPrice: Pricing.CurrentAmount,
            Currency: Pricing.Currency.Id,
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

        if (WinnerId is null)
            return AuctionErrors.Auction.InvalidState(Status.Id, "mark as sold without a winner");

        if (!Pricing.ReserveMet)
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

    /// <summary>
    /// Transitions a Sold auction to Completed once delivery is confirmed and no dispute is open.
    /// Cross-aggregate orchestration (dispute-freshness check, advisory lock, idempotency) is the
    /// caller's responsibility — see <c>AuctionCompletionOnOrderCompletedHandler</c> and
    /// <c>AuctionAutoCompleteJob</c>. The domain still re-validates every guard for defense in depth.
    /// </summary>
    /// <param name="nowUtc">Wall-clock time used for <see cref="ModifiedAt"/> and event <c>OccurredAt</c>.</param>
    /// <param name="deliveredAt">When the underlying shipment was confirmed delivered.</param>
    /// <param name="hasOpenDispute">
    /// Caller-supplied snapshot of dispute state read in the same unit-of-work.
    /// If true the transition is rejected so the job can retry after the dispute resolves.
    /// </param>
    public UnitResult<Error> MarkCompleted(DateTime nowUtc, DateTime deliveredAt, bool hasOpenDispute)
    {
        if (Item.Status != ItemStatus.Sold)
            return Error.Validation(
                "Item.Status",
                "Auction.MarkCompleted.ItemNotSold",
                "Cannot complete auction when the underlying item is not in the Sold state.");

        if (WinnerId is null)
            return Error.Validation(
                "WinnerId",
                "Auction.MarkCompleted.NoWinner",
                "Cannot complete auction without a resolved winner.");

        if (hasOpenDispute)
            return Error.Conflict(
                "Auction.MarkCompleted.DisputeOpen",
                "Cannot complete auction while a dispute is open.");

        var transitionResult = EnsureCanTransition(AuctionStatus.Completed);
        if (transitionResult.IsFailure)
            return transitionResult.Error;

        Status = AuctionStatus.Completed;
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionCompletedEvent(
            AuctionId: $"{Id}",
            WinnerId: $"{WinnerId}",
            SellerId: $"{Item.SellerId}",
            FinalPrice: Pricing.CurrentAmount,
            Currency: Pricing.Currency.Id,
            DeliveredAt: deliveredAt,
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> CancelAuction(string reason, DateTime nowUtc, bool isAdminOverride = false)
    {
        if (!Status.CanTransitionTo(AuctionStatus.Cancelled))
            return AuctionErrors.Auction.InvalidState(Status.Id, "cancel");

        // SECURITY: Block sellers from yanking a live auction with active bids.
        // Active or Winning bids represent real bidder commitment; cancelling without
        // an explicit admin override would betray bidder trust. Admins must use
        // the AuctionEmergency / Terminate path (with audit trail) for hot auctions.
        if (Status == AuctionStatus.Active && !isAdminOverride)
        {
            var activeBidCount = _bids.Count(b =>
                b.Status == BidStatus.Active || b.Status == BidStatus.Winning);
            if (activeBidCount > 0)
                return AuctionErrors.Auction.CancelBlockedActiveBids(Id, activeBidCount);
        }

        Status = AuctionStatus.Cancelled;
        ActualEndTime = nowUtc;
        ModifiedAt = nowUtc;

        foreach (var bid in _bids.Where(b =>
                     b.Status == BidStatus.Active || b.Status == BidStatus.Winning))
        {
            bid.Cancel();
        }

        TerminalizeAllAutoBids(nowUtc);

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

        result = EnsureNotLockedByBuyNowReservation(nowUtc);

        if (result.IsFailure)
        {
            return result.Error;
        }

        result = EnsureLiveBiddingSupported();

        if (result.IsFailure)
        {
            return result.Error;
        }

        result = EnsureNotSeller(bidderId);
        
        if (result.IsFailure)
        {
            return result.Error;
        }

        result = EnsureBidderEligible(bidderId, nowUtc);

        if (result.IsFailure)
        {
            return result.Error;
        }

        var minimumBid = GetMinimumBidAmount();
        var previousHighestBid = Pricing.CurrentAmount;

        if (amount < minimumBid)
            return AuctionErrors.Bid.TooLow(amount, minimumBid);

        // Buy-now cap: any bid that meets or exceeds the buy-now ceiling settles the
        // auction at exactly buyNowPrice, not at the raw overbid amount. This keeps
        // order pricing, checkout, notifications and read models aligned with the
        // dedicated Buy Now flow.
        if (Pricing.HasBuyNowPrice && amount.Amount > Pricing.BuyNowAmount!.Value)
        {
            amount = Money.Of(Pricing.BuyNowAmount.Value, amount.Currency);
        }

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
                    OutbidAmount: Pricing.CurrentAmount,
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
        result = UpdatePriceAndCount(amount.Amount, bid.Id, nowUtc);

        if (result.IsFailure)
        {
            return result.Error;
        }

        // Buy-now-by-bid: if bid >= buyNowPrice, end auction immediately
        if (Pricing.HasBuyNowPrice && amount.Amount >= Pricing.BuyNowAmount!.Value)
        {
            bid.MarkAsWon();
            Status = AuctionStatus.Sold;
            ActualEndTime = nowUtc;
            WinnerId = bidderId;
            ModifiedAt = nowUtc;

            // Mark all other active bids as outbid
            foreach (var otherBid in _bids.Where(b => b.Id != bid.Id && b.Status == Enums.BidStatus.Winning))
                otherBid.MarkAsOutbid();

            // Cancel all auto-bids
            foreach (var ab in _autoBids.Where(ab => ab.IsEnabled))
                ab.MarkAsOutbid(nowUtc);

            RaiseDomainEvent(new BidPlacedEvent(
                AuctionId: $"{Id}",
                BidId: $"{bid.Id}",
                BidderId: $"{bidderId}",
                Amount: amount.Amount,
                PreviousHighestBid: previousHighestBid,
                IsAutoBid: false,
                PreviousBidderId: previousBidderId?.ToString(),
                BidCount: BidCount,
                BidTime: bid.CreatedAt,
                nowUtc));

            RaiseDomainEvent(new AuctionSoldEvent(
                AuctionId: $"{Id}",
                WinnerId: $"{bidderId}",
                SellerId: $"{Item.SellerId}",
                FinalPrice: amount.Amount,
                Currency: Pricing.Currency.Id,
                TotalBids: BidCount,
                OccurredAt: nowUtc));

            return bid; // Early return — no auto-extend, no proxy resolution
        }

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
            PreviousHighestBid: previousHighestBid,
            IsAutoBid: false,
            PreviousBidderId:  previousBidderId?.ToString(),
            BidCount: BidCount,
            BidTime: bid.CreatedAt,
            nowUtc));

        // Proxy resolution: determine winner + resolved price in one pass (max 1 counter-bid)
        result = ResolveProxyBids(excludeBidderId: bidderId, nowUtc,
            extensionThresholdMinutes, maxExtensions, maxDuration);

        if (result.IsFailure)
        {
            return result.Error;
        }

        // Check if proxy resolution resulted in buy-now price being reached
        if (Pricing.HasBuyNowPrice && Pricing.CurrentAmount >= Pricing.BuyNowAmount!.Value)
        {
            var winner = GetCurrentWinningBid();
            if (winner is not null)
            {
                winner.MarkAsWon();
                Status = AuctionStatus.Sold;
                ActualEndTime = nowUtc;
                WinnerId = winner.BidderId;
                ModifiedAt = nowUtc;

                foreach (var otherBid in _bids.Where(b => b.Id != winner.Id && b.Status == Enums.BidStatus.Winning))
                    otherBid.MarkAsOutbid();
                foreach (var ab in _autoBids.Where(ab => ab.IsEnabled))
                    ab.MarkAsOutbid(nowUtc);

                RaiseDomainEvent(new AuctionSoldEvent(
                    AuctionId: $"{Id}",
                    WinnerId: $"{winner.BidderId}",
                    SellerId: $"{Item.SellerId}",
                    FinalPrice: Pricing.CurrentAmount,
                    Currency: Pricing.Currency.Id,
                    TotalBids: BidCount,
                    OccurredAt: nowUtc));
            }
        }

        return bid;
    }
    
    public Result<AuctionBuyNowReservation, Error> InitiateBuyNowReservation(
        UserId buyerId,
        DateTime nowUtc,
        TimeSpan reservationWindow)
    {
        var result = EnsureCanInitiateBuyNow(buyerId, nowUtc);
        if (result.IsFailure)
            return result.Error;

        var qualificationResult = EnsureBuyerQualifiedForBuyNow(buyerId, nowUtc);
        if (qualificationResult.IsFailure)
            return qualificationResult.Error;

        var buyNowPrice = Pricing.BuyNowPrice!;
        var heldDeposit = _deposits.FirstOrDefault(d => d.BidderId == buyerId && d.IsHeld);
        var depositAmount = heldDeposit?.Amount ?? Money.Zero(buyNowPrice.Currency);
        var appliedDepositAmount = Math.Min(depositAmount.Amount, buyNowPrice.Amount);
        var gatewayAmountDue = buyNowPrice.Amount - appliedDepositAmount;

        var depositMoneyResult = Money.Create(appliedDepositAmount, buyNowPrice.Currency);
        if (depositMoneyResult.IsFailure)
            return depositMoneyResult.Error;

        var dueMoneyResult = Money.Create(gatewayAmountDue, buyNowPrice.Currency);
        if (dueMoneyResult.IsFailure)
            return dueMoneyResult.Error;

        var reservationResult = AuctionBuyNowReservation.Create(
            auctionId: Id,
            buyerId: buyerId,
            buyNowPrice: buyNowPrice,
            depositAppliedAmount: depositMoneyResult.Value,
            gatewayAmountDue: dueMoneyResult.Value,
            expiresAt: nowUtc.Add(reservationWindow),
            nowUtc: nowUtc);

        if (reservationResult.IsFailure)
            return reservationResult.Error;

        _buyNowReservations.Add(reservationResult.Value);
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionBuyNowReservedEvent(
            AuctionId: $"{Id}",
            ReservationId: $"{reservationResult.Value.Id}",
            BuyerId: $"{buyerId}",
            BuyNowPrice: buyNowPrice.Amount,
            DepositAppliedAmount: appliedDepositAmount,
            AmountDue: gatewayAmountDue,
            ExpiresAt: reservationResult.Value.ExpiresAt,
            OccurredAt: nowUtc));

        return reservationResult.Value;
    }

    public Result<AuctionBuyNowReservation, Error> AttachBuyNowPayment(
        AuctionBuyNowReservationId reservationId,
        TransactionId paymentTransactionId,
        DateTime nowUtc)
    {
        var reservation = FindBuyNowReservation(reservationId);
        if (reservation.IsFailure)
            return reservation.Error;

        var attachResult = reservation.Value.AttachPayment(paymentTransactionId, nowUtc);
        if (attachResult.IsFailure)
            return attachResult.Error;

        ModifiedAt = nowUtc;
        return reservation.Value;
    }

    public UnitResult<Error> ExpireBuyNowReservation(
        AuctionBuyNowReservationId reservationId,
        DateTime nowUtc)
    {
        var reservation = FindBuyNowReservation(reservationId);
        if (reservation.IsFailure)
            return reservation.Error;

        if (!reservation.Value.IsPendingPayment)
            return UnitResult.Success<Error>();

        var expireResult = reservation.Value.Expire(nowUtc);
        if (expireResult.IsFailure)
            return expireResult.Error;

        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionBuyNowReservationReleasedEvent(
            AuctionId: $"{Id}",
            ReservationId: $"{reservationId}",
            BuyerId: $"{reservation.Value.BuyerId}",
            Reason: "expired",
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> FailBuyNowReservation(
        AuctionBuyNowReservationId reservationId,
        string reason,
        DateTime nowUtc)
    {
        var reservation = FindBuyNowReservation(reservationId);
        if (reservation.IsFailure)
            return reservation.Error;

        if (!reservation.Value.IsPendingPayment)
            return UnitResult.Success<Error>();

        var failResult = reservation.Value.Fail(reason, nowUtc);
        if (failResult.IsFailure)
            return failResult.Error;

        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionBuyNowReservationReleasedEvent(
            AuctionId: $"{Id}",
            ReservationId: $"{reservationId}",
            BuyerId: $"{reservation.Value.BuyerId}",
            Reason: reason,
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }

    public Result<Bid, Error> FinalizeBuyNowReservation(
        AuctionBuyNowReservationId reservationId,
        DateTime nowUtc,
        IPAddress? ipAddress = null)
    {
        var reservation = FindBuyNowReservation(reservationId);
        if (reservation.IsFailure)
            return reservation.Error;

        if (!reservation.Value.IsPendingPayment)
            return AuctionErrors.BuyNowReservation.InvalidState(reservation.Value.Status.Id, "finalize");

        if (!reservation.Value.IsActive(nowUtc))
            return AuctionErrors.BuyNowReservation.Expired;

        if (Pricing.BuyNowAmount is null || !Pricing.IsBuyNowAvailable)
            return AuctionErrors.Auction.NotSupportBuyNow;

        foreach (var existingBid in _bids.Where(b =>
                     b.Status == BidStatus.Active || b.Status == BidStatus.Winning))
        {
            existingBid.Cancel();
        }

        var bid = Bid.Create(Id, reservation.Value.BuyerId, Pricing.BuyNowPrice!, autoBidId: null, ipAddress, nowUtc);
        bid.MarkAsWon();
        _bids.Add(bid);

        var buyNowResult = Pricing.WithBuyNow();
        if (buyNowResult.IsFailure)
            return buyNowResult.Error;

        var reservationPaidResult = reservation.Value.MarkPaid(nowUtc);
        if (reservationPaidResult.IsFailure)
            return reservationPaidResult.Error;

        Pricing = buyNowResult.Value;
        WinnerId = reservation.Value.BuyerId;
        SyncAutoBidStateToWinner(reservation.Value.BuyerId, nowUtc);
        BidCount++;
        ActualEndTime = nowUtc;
        Status = AuctionStatus.Sold;
        ModifiedAt = nowUtc;

        _priceHistories.Add(AuctionPriceHistory.CreateBuyNow(Id, Pricing.BuyNowPrice!, nowUtc, bid.Id));

        RaiseDomainEvent(new AuctionSoldEvent(
            AuctionId: $"{Id}",
            WinnerId: $"{reservation.Value.BuyerId}",
            SellerId: $"{Item.SellerId}",
            FinalPrice: Pricing.BuyNowAmount!.Value,
            Currency: Pricing.Currency.Id,
            TotalBids: BidCount,
            OccurredAt: nowUtc));

        RaiseDomainEvent(new BuyNowExecutedEvent(
            $"{Id}",
            $"{reservation.Value.BuyerId}",
            Pricing.BuyNowAmount!.Value,
            nowUtc));

        return bid;
    }

    public UnitResult<Error> LinkBuyNowReservationOrder(
        AuctionBuyNowReservationId reservationId,
        OrderId orderId,
        DateTime nowUtc)
    {
        var reservation = FindBuyNowReservation(reservationId);
        if (reservation.IsFailure)
            return reservation.Error;

        var result = reservation.Value.LinkOrder(orderId, nowUtc);
        if (result.IsFailure)
            return result.Error;

        ModifiedAt = nowUtc;
        return UnitResult.Success<Error>();
    }

    // ==================================================================================
    //                              AUTO BIDDING
    // ==================================================================================
    public UnitResult<Error> ValidateAutoBidConfiguration(
        UserId bidderId,
        Money maxAmount,
        DateTime nowUtc,
        Money? incrementAmount = null)
    {
        var result = EnsureAcceptsBids(nowUtc);

        if (result.IsFailure)
            return result.Error;

        result = EnsureNotLockedByBuyNowReservation(nowUtc);

        if (result.IsFailure)
            return result.Error;

        result = EnsureLiveBiddingSupported();

        if (result.IsFailure)
            return result.Error;

        result = EnsureNotSeller(bidderId);

        if (result.IsFailure)
            return result.Error;

        result = EnsureBidderEligible(bidderId, nowUtc);

        if (result.IsFailure)
            return result.Error;

        // Ensure currency matches auction currency before any Money comparison
        if (maxAmount.Currency != Pricing.Currency)
            return Money.Errors.CurrencyMismatch(Pricing.Currency.Id, maxAmount.Currency.Id);

        if (incrementAmount is not null && incrementAmount.Currency != Pricing.Currency)
            return Money.Errors.CurrencyMismatch(Pricing.Currency.Id, incrementAmount.Currency.Id);

        var validationResult = AutoBid.Check(isInvariant: true)
            .Field(maxAmount, x => x.Budget.MaxAmount)
            .GreaterThanOrEqual(GetMinimumBidAmount())
            .Field(incrementAmount, x => x.Budget.IncrementAmount)
            .WhenHasValue(x => x.GreaterThan(Money.Zero(maxAmount.Currency)))
            .ToResult();

        if (validationResult.IsFailure)
            return validationResult.Error;

        var existing = _autoBids.FirstOrDefault(ab => ab.BidderId == bidderId);
        if (existing is not null)
        {
            if (!existing.IsEnabled &&
                existing.Status != AutoBidStatus.Exhausted &&
                existing.Status != AutoBidStatus.Outbid)
            {
                return AuctionErrors.AutoBid.IsDisabled;
            }

            if (existing.Status == AutoBidStatus.Won)
                return AuctionErrors.AutoBid.CannotModifyFinalStatus;

            if (maxAmount.Amount < existing.Budget.CurrentAmount)
                return AuctionErrors.AutoBid.NewMaxLessThanCurrent(existing.Budget.CurrentAmount);

            var updatePreview = existing.Budget.WithConfiguration(maxAmount, incrementAmount);
            return updatePreview.IsFailure
                ? updatePreview.Error
                : UnitResult.Success<Error>();
        }

        var createPreview = AutoBidBudget.Create(maxAmount.Amount, Pricing.Currency, incrementAmount?.Amount);
        return createPreview.IsFailure
            ? createPreview.Error
            : UnitResult.Success<Error>();
    }

    public Result<AutoBid, Error> ConfigureAutoBid(
        UserId bidderId,
        Money maxAmount,
        DateTime nowUtc,
        Money? incrementAmount = null)
    {
        var validationResult = ValidateAutoBidConfiguration(
            bidderId,
            maxAmount,
            nowUtc,
            incrementAmount);

        if (validationResult.IsFailure)
            return validationResult.Error;
        
        // Check existing auto-bid (UNIQUE constraint: auction_id + bidder_id)
        var existing = _autoBids.FirstOrDefault(ab => ab.BidderId == bidderId);

        if (existing is not null)
        {
            var resultUpdateExiting = existing.UpdateConfig(maxAmount, incrementAmount, nowUtc);
            
            if (resultUpdateExiting.IsFailure)
            {
                return resultUpdateExiting.Error;
            }

            var engageExistingResult = EngageAutoBidNow(existing, bidderId, nowUtc);
            if (engageExistingResult.IsFailure)
                return engageExistingResult.Error;

            return existing;
        }

        var budget = AutoBidBudget.Create(maxAmount.Amount, Pricing.Currency, incrementAmount?.Amount);

        if (budget.IsFailure)
        {
            return budget.Error;
        }

        var autoBid = AutoBid.Create(
            Id,
            bidderId,
            budget.Value,
            nowUtc);
        
        _autoBids.Add(autoBid);

        RaiseDomainEvent(new AuctionAutoBidConfiguredEvent(
            $"{Id}", 
            $"{bidderId}",
            maxAmount.Amount,
            nowUtc));

        var engageNewResult = EngageAutoBidNow(autoBid, bidderId, nowUtc);
        if (engageNewResult.IsFailure)
            return engageNewResult.Error;

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

        return result.IsFailure ? result.Error : result;
    }

    public Result<AutoBid, Error> CancelAutoBid(
        UserId bidderId,
        DateTime nowUtc)
    {
        var (_, isFailure, autoBid, error) = FindAutoBid(bidderId);

        if (isFailure)
            return error;

        var cancelResult = autoBid.Cancel(nowUtc);

        return cancelResult.IsFailure ? cancelResult.Error : autoBid;
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

        result = EnsureNotLockedByBuyNowReservation(nowUtc);

        if (result.IsFailure)
        {
            return result.Error;
        }
        
        var (_, isFailure, autoBid, error) = FindAutoBid(bidderId);

        if (isFailure)
        {
            return error;
        }
        
        var resumeResult = autoBid.Resume(nowUtc);

        if (resumeResult.IsFailure)
        {
            return resumeResult.Error;
        }

        var engageResult = EngageAutoBidNow(autoBid, bidderId, nowUtc);

        return engageResult.IsFailure ? engageResult.Error : UnitResult.Success<Error>();
    }
    
    /// <summary>
    /// Proxy bidding resolution: determines the winner and resolved price in one pass.
    /// Creates at most ONE counter-bid instead of cascading individual bids.
    /// </summary>
    private UnitResult<Error> ResolveProxyBids(
        UserId excludeBidderId,
        DateTime nowUtc,
        TimeSpan extensionThresholdMinutes,
        int maxExtensions,
        TimeSpan maxDuration)
    {
        var bidIncrement = Pricing.BidIncrementAmount;
        var currentPrice = Pricing.CurrentAmount;
        var currency = Pricing.Currency;

        // 1. Collect all eligible auto-bids (excluding the manual bidder)
        var eligibleAutoBids = _autoBids
            .Where(ab => ab.BidderId != excludeBidderId &&
                         ab.IsEnabled &&
                         ab.Status == AutoBidStatus.Active)
            .OrderByDescending(ab => ab.Budget.MaxAmount)
            .ThenBy(ab => ab.CreatedAt)
            .ToList();

        if (eligibleAutoBids.Count == 0)
            return UnitResult.Success<Error>();

        // 2. Check if manual bidder also has an auto-bid (their ceiling)
        var manualBidderAutoBid = _autoBids
            .FirstOrDefault(ab => ab.BidderId == excludeBidderId &&
                                  ab.IsEnabled &&
                                  ab.Status == AutoBidStatus.Active);

        // 3. Determine winner: highest ceiling among all eligible auto-bids
        var winner = eligibleAutoBids[0]; // sorted DESC by MaxAmount
        var winnerCeiling = winner.Budget.MaxAmount;

        // 4. Determine runner-up ceiling
        //    = max of: current visible price, manual bidder's auto-bid ceiling, second-highest auto-bid ceiling
        var runnerUpCeiling = currentPrice;

        if (manualBidderAutoBid is not null && manualBidderAutoBid.Budget.MaxAmount > runnerUpCeiling)
            runnerUpCeiling = manualBidderAutoBid.Budget.MaxAmount;

        if (eligibleAutoBids.Count > 1 && eligibleAutoBids[1].Budget.MaxAmount > runnerUpCeiling)
            runnerUpCeiling = eligibleAutoBids[1].Budget.MaxAmount;

        // 5. Check if winner can actually beat the minimum next bid
        var minimumRequired = GetMinimumBidAmount();
        if (!winner.CanBid(minimumRequired))
        {
            // Winner can't afford → mark all as outbid, nobody counters
            foreach (var ab in eligibleAutoBids)
            {
                if (ab.Status == AutoBidStatus.Active)
                    ab.MarkAsOutbid(nowUtc);
            }
            return UnitResult.Success<Error>();
        }

        // 6. Resolve the visible price
        //    = min(winner ceiling, runner-up ceiling + increment)
        var resolvedAmount = Math.Min(winnerCeiling, runnerUpCeiling + bidIncrement);

        // Ensure resolved amount is at least the minimum required bid
        if (resolvedAmount < minimumRequired.Amount)
            resolvedAmount = minimumRequired.Amount;

        // Cap at winner's ceiling
        if (resolvedAmount > winnerCeiling)
            resolvedAmount = winnerCeiling;

        // Buy-now cap: auto-bid resolution may cross the buy-now threshold; when it does,
        // settle the auction at exactly buyNowPrice rather than the raw ceiling. This keeps
        // AuctionSoldEvent.FinalPrice and downstream order pricing aligned with Buy Now.
        if (Pricing.HasBuyNowPrice && resolvedAmount > Pricing.BuyNowAmount!.Value)
            resolvedAmount = Pricing.BuyNowAmount.Value;

        var resolvedPrice = Money.Of(resolvedAmount, currency);

        // 7. Place ONE counter-bid at the resolved price
        var counterBidTime = nowUtc.AddTicks(1); // slightly after manual bid for ordering
        var placeResult = PlaceAutoBidInternal(
            winner, resolvedPrice, counterBidTime,
            raiseOutbidEvent: true,
            extensionThresholdMinutes: extensionThresholdMinutes,
            maxExtensions: maxExtensions,
            maxDuration: maxDuration);

        if (placeResult.IsFailure)
            return placeResult.Error;

        // 8. Mark all OTHER auto-bids as outbid (they lost the proxy resolution)
        foreach (var ab in eligibleAutoBids)
        {
            if (ab.Id != winner.Id && ab.Status == AutoBidStatus.Active)
                ab.MarkAsOutbid(counterBidTime);
        }

        // 9. If manual bidder had an auto-bid and was outbid by winner, mark it
        if (manualBidderAutoBid is not null &&
            manualBidderAutoBid.Status == AutoBidStatus.Active &&
            manualBidderAutoBid.Budget.MaxAmount < resolvedAmount)
        {
            manualBidderAutoBid.MarkAsOutbid(counterBidTime);
        }

        return UnitResult.Success<Error>();
    }
    
    private UnitResult<Error> PlaceAutoBidInternal(
        AutoBid autoBid,
        Money bidAmount,
        DateTime nowUtc,
        bool raiseOutbidEvent = true,
        TimeSpan extensionThresholdMinutes = default,
        int maxExtensions = 0,
        TimeSpan maxDuration = default)
    {
        // 1. Validate BEFORE mutating — ensure pricing and budget are valid
        var pricingCheck = Pricing.WithNewBid(bidAmount.Amount, GetMinimumBidAmount().Amount);
        if (pricingCheck.IsFailure)
            return pricingCheck.Error;

        var budgetCheck = autoBid.Budget.WithBidPlaced(bidAmount);
        if (budgetCheck.IsFailure)
            return budgetCheck.Error;

        // 2. All checks passed — now mutate state
        var previousWinning = GetCurrentWinningBid();
        var previousBidderId = previousWinning?.BidderId;
        var previousHighestBid = Pricing.CurrentAmount;
        previousWinning?.MarkAsOutbid();

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

        Pricing = pricingCheck.Value;
        BidCount = _bids.Count(b => b.Status != Enums.BidStatus.Cancelled);
        ModifiedAt = nowUtc;
        _priceHistories.Add(AuctionPriceHistory.Create(Id, Pricing.CurrentPrice,AuctionPriceHistoryType.AutoBid, nowUtc, bid.Id));

        // 3. Events
        RaiseDomainEvent(new BidPlacedEvent(
            AuctionId: $"{Id}",
            BidId: $"{bid.Id}",
            BidderId: $"{autoBid.BidderId}",
            Amount: bidAmount.Amount,
            PreviousHighestBid: previousHighestBid,
            IsAutoBid: true,
            PreviousBidderId: previousBidderId?.ToString(),
            BidCount: BidCount,
            BidTime: bid.CreatedAt,
            nowUtc));

        if (raiseOutbidEvent && previousBidderId.HasValue && previousBidderId.Value != autoBid.BidderId)
        {
            RaiseDomainEvent(new OutbidEvent(
                AuctionId: $"{Id}",
                OutbidBidderId: $"{previousBidderId}",
                NewHighBidderId: $"{autoBid.BidderId}",
                NewHighestBid: bidAmount.Amount,
                OutbidAmount: Pricing.CurrentAmount,
                OccurredAt: nowUtc));
        }

        // Auto-extend check (mirrors PlaceBid behavior)
        if (extensionThresholdMinutes != default)
        {
            var extendResult = TryAutoExtend(
                nowUtc: nowUtc,
                extensionThresholdMinutes: extensionThresholdMinutes,
                maxExtensions: maxExtensions,
                maxDuration: maxDuration,
                triggerByBidId: bid.Id);

            if (extendResult.IsFailure)
                return extendResult.Error;
        }

        return UnitResult.Success<Error>();
    }

    // ==================================================================================
    //                          PARTICIPATION / QUALIFICATION
    // ==================================================================================
    public Result<AuctionParticipant, Error> RegisterParticipantFromDeposit(
        UserId userId,
        DateTime nowUtc,
        string roleInAuction = "bidder")
    {
        if (userId == Item.SellerId)
            return AuctionErrors.Auction.SelfBid;

        if (Info is null)
            return AuctionErrors.Auction.TimingRequired;

        if (!Info.HasQualification || !Info.IsQualificationOpen(nowUtc))
            return AuctionErrors.Participant.JoinWindowClosed;

        var existingParticipant = _participants
            .FirstOrDefault(p => p.UserId == userId && p.JoinStatus != ParticipantJoinStatus.Withdrawn);

        if (existingParticipant is not null)
        {
            // Ensure participant is qualified — deposit confirms eligibility
            if (!existingParticipant.IsQualified)
                existingParticipant.Qualify(nowUtc);
            return existingParticipant;
        }

        var participant = AuctionParticipant.Create(
            Id,
            userId,
            roleInAuction,
            nowUtc: nowUtc);

        _participants.Add(participant);
        ModifiedAt = nowUtc;

        return participant;
    }

    // ==================================================================================
    //                              SEALED BIDS
    // ==================================================================================
    public Result<SealedBid, Error> SubmitSealedBid(
        UserId bidderId,
        string amountEncrypted,
        DateTime nowUtc)
    {
        if (AuctionType != AuctionType.Sealed)
            return AuctionErrors.SealedBid.OnlySupportedForSealedAuction;

        var result = EnsureAcceptsBids(nowUtc);
        if (result.IsFailure)
            return result.Error;

        result = EnsureNotSeller(bidderId);
        if (result.IsFailure)
            return result.Error;

        result = EnsureBidderEligible(bidderId, nowUtc);
        if (result.IsFailure)
            return result.Error;

        if (_sealedBids.Any(sb => sb.BidderId == bidderId && sb.Status == SealedBidStatus.Submitted))
            return AuctionErrors.SealedBid.AlreadySubmitted;

        var sealedBid = SealedBid.Submit(Id, bidderId, amountEncrypted, nowUtc);
        _sealedBids.Add(sealedBid);
        ModifiedAt = nowUtc;

        return sealedBid;
    }

    public Result<SealedBid, Error> RevealSealedBid(
        SealedBidId sealedBidId,
        UserId actorId,
        DateTime nowUtc)
    {
        if (AuctionType != AuctionType.Sealed)
            return AuctionErrors.SealedBid.OnlySupportedForSealedAuction;

        if (Status == AuctionStatus.Active || (Info is not null && !Info.HasEnded(nowUtc)))
            return AuctionErrors.SealedBid.RevealNotAllowed;

        var sealedBid = _sealedBids.FirstOrDefault(sb => sb.Id == sealedBidId);

        if (sealedBid is null)
            return AuctionErrors.SealedBid.NotFound;

        sealedBid.Reveal(actorId, nowUtc);
        ModifiedAt = nowUtc;

        return sealedBid;
    }

    public Result<IReadOnlyCollection<SealedBid>, Error> RevealAllSealedBids(
        IReadOnlyCollection<RevealedSealedBidAmount> revealedBids,
        UserId? actorId,
        DateTime nowUtc)
    {
        if (AuctionType != AuctionType.Sealed)
            return AuctionErrors.SealedBid.OnlySupportedForSealedAuction;

        if (Status == AuctionStatus.Active && (Info is null || !Info.HasEnded(nowUtc)))
            return AuctionErrors.SealedBid.RevealNotAllowed;

        var revealedAmountMap = revealedBids.ToDictionary(x => x.SealedBidId, x => x.Amount);

        foreach (var sealedBid in _sealedBids)
        {
            if (!revealedAmountMap.ContainsKey(sealedBid.Id))
                return AuctionErrors.SealedBid.NotFound;

            if (sealedBid.Status == SealedBidStatus.Submitted)
            {
                sealedBid.Reveal(actorId, nowUtc);
            }
        }

        if (_bids.Count > 0)
        {
            ModifiedAt = nowUtc;
            return _sealedBids.AsReadOnly();
        }

        var materializedBids = _sealedBids
            .Where(sb => sb.Status == SealedBidStatus.Revealed)
            .Select(sb => new
            {
                SealedBid = sb,
                Amount = revealedAmountMap[sb.Id]
            })
            .Where(x => x.Amount.Amount >= Pricing.StartingAmount)
            .OrderByDescending(x => x.Amount.Amount)
            .ThenBy(x => x.SealedBid.CreatedAt)
            .ToList();

        if (materializedBids.Count == 0)
        {
            ModifiedAt = nowUtc;
            return _sealedBids.AsReadOnly();
        }

        Bid? winningBid = null;

        foreach (var materializedBid in materializedBids)
        {
            var bid = Bid.Create(
                auctionId: Id,
                bidderId: materializedBid.SealedBid.BidderId,
                amount: materializedBid.Amount,
                autoBidId: null,
                ipAddress: null,
                nowUtc: nowUtc,
                createdAt: materializedBid.SealedBid.CreatedAt);

            if (winningBid is null)
            {
                bid.MarkAsWinning();
                winningBid = bid;
            }
            else
            {
                bid.MarkAsOutbid();
            }

            _bids.Add(bid);
        }

        var winningBidValue = winningBid!;
        var pricingResult = Pricing.WithNewBid(winningBidValue.Amount.Amount, Pricing.StartingAmount);

        if (pricingResult.IsFailure)
        {
            return pricingResult.Error;
        }

        Pricing = pricingResult.Value;
        BidCount += materializedBids.Count;
        _priceHistories.Add(AuctionPriceHistory.CreateSealedBid(Id, winningBidValue.Amount, nowUtc, winningBidValue.Id));
        ModifiedAt = nowUtc;

        return _sealedBids.AsReadOnly();
    }

    // ==================================================================================
    //                              EMERGENCY CONTROL
    // ==================================================================================
    public Result<AuctionEmergency, Error> TriggerEmergency(
        UserId? actorId,
        string triggerSource,
        string reason,
        string payload,
        DateTime nowUtc)
    {
        if (_emergencies.Any(e => e.Status == EmergencyStatus.Triggered))
            return AuctionErrors.Emergency.AlreadyTriggered;

        var emergency = AuctionEmergency.Create(
            Id,
            actorId,
            triggerSource,
            reason,
            payload,
            nowUtc);

        _emergencies.Add(emergency);
        ModifiedAt = nowUtc;

        return emergency;
    }

    public Result<AuctionEmergency, Error> ResolveEmergency(
        AuctionEmergencyId emergencyId,
        EmergencyStatus status,
        string payload,
        DateTime nowUtc)
    {
        var emergency = _emergencies.FirstOrDefault(e => e.Id == emergencyId);

        if (emergency is null)
            return AuctionErrors.Emergency.NotFound;

        emergency.MoveTo(status, status.Id, payload, nowUtc);
        ModifiedAt = nowUtc;

        return emergency;
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
        
        
        if (userId == Item.SellerId)
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

        RaiseDomainEvent(new AuctionWatcherRemovedEvent(
            $"{Id}",
            $"{userId}",
            nowUtc));
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

        if (userId == Item.SellerId)
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
            ? Pricing.StartingPrice
            : Pricing.NextMinimumBid;
    }
    
    public Bid? GetCurrentWinningBid()
    {
        return _bids
            .Where(b => b.Status == BidStatus.Winning)
            .MaxBy(b => b.Amount.Amount);
    }

    public AuctionBuyNowReservation? GetActiveBuyNowReservation(DateTime nowUtc)
    {
        return _buyNowReservations
            .Where(r => r.IsActive(nowUtc))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefault();
    }

    public Result<Bid, Error> CancelBidByAdmin(BidId bidId, DateTime nowUtc)
    {
        if (Status != AuctionStatus.Active && Status != AuctionStatus.Ended)
            return AuctionErrors.Auction.InvalidState(Status.Id, "cancel invalid bid");

        var bid = _bids.FirstOrDefault(x => x.Id == bidId);
        if (bid is null)
            return AuctionErrors.Bid.NotFound(bidId);

        if (bid.Status == BidStatus.Cancelled)
            return AuctionErrors.Bid.BidAlreadyOutbid;

        bid.Cancel();

        var remainingBids = _bids
            .Where(x => x.Status != BidStatus.Cancelled)
            .OrderByDescending(x => x.Amount.Amount)
            .ThenBy(x => x.CreatedAt)
            .ToList();

        BidCount = remainingBids.Count;

        if (remainingBids.Count == 0)
        {
            var pricingResult = AuctionPricing.Create(
                startingPrice: Pricing.StartingAmount,
                bidIncrement: Pricing.BidIncrementAmount,
                currency: Pricing.Currency,
                reservePrice: Pricing.ReserveAmount,
                buyNowPrice: Pricing.BuyNowAmount);

            if (pricingResult.IsFailure)
                return pricingResult.Error;

            Pricing = pricingResult.Value;
            WinnerId = null;
            ModifiedAt = nowUtc;
            _priceHistories.Add(AuctionPriceHistory.CreateResetToStartingPrice(Id, Pricing.CurrentPrice, nowUtc));
            return bid;
        }

        foreach (var remaining in remainingBids)
        {
            remaining.MarkAsOutbid();
        }

        var highestBid = remainingBids[0];

        if (Status == AuctionStatus.Active)
        {
            highestBid.MarkAsWinning();
            WinnerId = null;
        }
        else
        {
            highestBid.MarkAsWon();
            WinnerId = highestBid.BidderId;
        }

        var repricingResult = Pricing.WithNewBid(highestBid.Amount.Amount, Pricing.StartingAmount);
        if (repricingResult.IsFailure)
            return repricingResult.Error;

        Pricing = repricingResult.Value;
        ModifiedAt = nowUtc;
        _priceHistories.Add(AuctionPriceHistory.CreateRepricedAfterBidCancellation(Id, Pricing.CurrentPrice, nowUtc, highestBid.Id));

        return bid;
    }

    public bool IsEndingSoon(DateTime nowUtc, TimeSpan extensionThresholdMinutes) =>
        Status == AuctionStatus.Active &&
        Info is not null &&
        Info.RemainingTime(nowUtc) <= extensionThresholdMinutes;
    
    // ==================================================================================
    //                              PRIVATE HELPERS
    // ==================================================================================
    private UnitResult<Error> UpdatePriceAndCount(
        decimal newPrice,
        BidId bidId,
        DateTime nowUtc)
    {
        var result = Pricing.WithNewBid(newPrice, GetMinimumBidAmount().Amount);

        if (result.IsFailure)
        {
            return result.Error;
        }
        
        Pricing = result.Value;
        BidCount = _bids.Count(b => b.Status != BidStatus.Cancelled);
        ModifiedAt = nowUtc;
        _priceHistories.Add(AuctionPriceHistory.CreateBid(Id, Pricing.CurrentPrice, nowUtc, bidId));

        return UnitResult.Success<Error>();
    }
    
    private UnitResult<Error> TryAutoExtend(
        DateTime nowUtc,
        TimeSpan extensionThresholdMinutes, 
        int maxExtensions,
        TimeSpan maxDuration,
        BidId triggerByBidId)
    {
        if (Info is null || !Info.AutoExtend || Info.ExtensionCount >= maxExtensions || !IsEndingSoon(nowUtc, extensionThresholdMinutes)) 
            return UnitResult.Success<Error>();

        var oldEndTime = Info.EndTime;
        
        var durationResult = Info.Extend(maxDuration);
        if (durationResult.IsFailure)
        {
            return durationResult.Error;
        }

        Info = durationResult.Value ;
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionExtendedEvent(
            AuctionId: $"{Id}",
            TriggerByBidId: $"{triggerByBidId}",
            PreviousEndTime: oldEndTime, 
            NewEndTime: Info.EndTime,
            ExtensionMinutes: Info.ExtensionMinutes,
            ExtensionCount: Info.ExtensionCount,
            OccurredAt: nowUtc));
        
        return UnitResult.Success<Error>();
    }
    
    private UnitResult<Error>  EnsureAcceptsBids(DateTime nowUtc)
    {
        if (!Status.AcceptsBids)
            return AuctionErrors.Auction.InvalidState(Status.Id, "place bid");

        if (Info is null)
            return AuctionErrors.Auction.TimingRequired;

        if (Info.HasEnded(nowUtc))
            return AuctionErrors.Auction.Expired;
        
        return UnitResult.Success<Error>();
    }

    private UnitResult<Error> EnsureLiveBiddingSupported()
    {
        return AuctionType == AuctionType.Sealed
            ? AuctionErrors.Bid.LiveBiddingUnavailableForSealedAuction
            : UnitResult.Success<Error>();
    }

    private UnitResult<Error> EnsureBidderEligible(UserId bidderId, DateTime nowUtc)
    {
        var participant = _participants.FirstOrDefault(p => p.UserId == bidderId);

        if (participant is null || !IsBidEligibleParticipant(participant, nowUtc))
            return AuctionErrors.Participant.NotQualified;

        return UnitResult.Success<Error>();
    }

    private UnitResult<Error> EnsureCanInitiateBuyNow(UserId buyerId, DateTime nowUtc)
    {
        if (Pricing.BuyNowAmount is null || !Pricing.IsBuyNowAvailable)
            return AuctionErrors.Auction.NotSupportBuyNow;

        var liveReservation = GetActiveBuyNowReservation(nowUtc);
        if (liveReservation is not null)
            return AuctionErrors.Auction.BuyNowReservationActive;

        var sellerResult = EnsureNotSeller(buyerId);
        if (sellerResult.IsFailure)
            return sellerResult.Error;

        if (Status == AuctionStatus.Scheduled)
        {
            if (Info is null)
                return AuctionErrors.Auction.TimingRequired;

            if (!Info.HasQualification)
                return AuctionErrors.Auction.QualificationWindowRequired;

            if (Info.IsQualificationClosed(nowUtc))
                return AuctionErrors.Auction.BuyNowQualificationClosed;

            if (!Info.IsQualificationOpen(nowUtc))
                return AuctionErrors.Auction.BuyNowQualificationNotOpenYet;
        }
        else if (Status != AuctionStatus.Active)
        {
            return AuctionErrors.Auction.BuyNowUnavailableForScheduledAuction;
        }

        return UnitResult.Success<Error>();
    }

    private UnitResult<Error> EnsureBuyerQualifiedForBuyNow(UserId buyerId, DateTime nowUtc)
    {
        var participant = _participants
            .FirstOrDefault(p => p.UserId == buyerId && p.JoinStatus != ParticipantJoinStatus.Withdrawn);

        // In Scheduled status (deposit/qualification stage), we allow anyone to Buy Now.
        // We auto-create or qualify the participant as needed.
        if (Status == AuctionStatus.Scheduled)
        {
            if (participant is null)
            {
                _participants.Add(AuctionParticipant.Create(Id, buyerId, "buy_now", nowUtc));
                ModifiedAt = nowUtc;
                return UnitResult.Success<Error>();
            }

            if (!participant.IsQualified)
            {
                participant.Qualify(nowUtc);
                ModifiedAt = nowUtc;
            }

            return UnitResult.Success<Error>();
        }

        // In Active status (bidding stage), we strictly require that the buyer is already a qualified participant.
        if (participant is null || !participant.IsQualified)
        {
            return AuctionErrors.Participant.NotQualified;
        }

        return UnitResult.Success<Error>();
    }

    private UnitResult<Error> EnsureNotLockedByBuyNowReservation(DateTime nowUtc)
    {
        return GetActiveBuyNowReservation(nowUtc) is null
            ? UnitResult.Success<Error>()
            : AuctionErrors.Auction.BuyNowReservationActive;
    }

    public UnitResult<Error> Terminate(string reason, DateTime nowUtc)
    {
        var result = EnsureCanTransition(AuctionStatus.Terminated);
        if (result.IsFailure)
            return result.Error;

        Status = AuctionStatus.Terminated;
        ActualEndTime = nowUtc;
        WinnerId = null;
        ModifiedAt = nowUtc;

        foreach (var bid in _bids.Where(b => b.Status == BidStatus.Active || b.Status == BidStatus.Winning))
        {
            bid.Cancel();
        }

        TerminalizeAllAutoBids(nowUtc);

        foreach (var offer in _winnerOffers.Where(x => x.OfferStatus == WinnerOfferStatus.Pending))
        {
            offer.Cancel(nowUtc);
        }

        RaiseDomainEvent(new AuctionTerminatedEvent(
            AuctionId: $"{Id}",
            SellerId: $"{Item.SellerId}",
            Reason: reason,
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> MarkPaymentDefaulted(DateTime nowUtc)
    {
        if (WinnerId is null)
            return AuctionErrors.Auction.PaymentDefaultRequiresWinner;

        var result = EnsureCanTransition(AuctionStatus.PaymentDefaulted);
        if (result.IsFailure)
            return result.Error;

        Status = AuctionStatus.PaymentDefaulted;
        ModifiedAt = nowUtc;

        // Auto-forfeit the defaulting winner's held deposit. Idempotent: if the deposit is no
        // longer Held (e.g. already Forfeited by a previous run, or never created), skip.
        // Without this, deposits would hang in Held forever unless an operator manually
        // invoked ForfeitAuctionDepositCommand.
        // Auto-forfeit the defaulting winner's held deposit.
        var winnerDeposit = _deposits.FirstOrDefault(d =>
            d.BidderId == WinnerId && d.Status == DepositStatus.Held);
        if (winnerDeposit is not null)
        {
            var forfeitResult = winnerDeposit.Forfeit(nowUtc);
            if (forfeitResult.IsFailure)
                return forfeitResult.Error;
        }

        // Auto-record the defaulted winner in the WinnerOffers collection if not already present.
        // This ensures they are tracked and excluded from future runner-up offers.
        var existingOffer = _winnerOffers.FirstOrDefault(o => o.UserId == WinnerId);
        if (existingOffer is not null)
        {
            existingOffer.MarkAsDefaulted(nowUtc);
        }
        else
        {
            // If they weren't in WinnerOffers, they were likely the original winner.
            // We create a record for them to ensure they are excluded from runner-up offers.
            UserId winnerId = (UserId)WinnerId!;
            var rankedBids = GetRankedBids();
            var rankNo = rankedBids
                .Select((bid, index) => new { bid.BidderId, Rank = index + 1 })
                .FirstOrDefault(x => x.BidderId == winnerId)?.Rank ?? 1;

            var defaultRecord = AuctionWinnerOffer.Create(
                auctionId: Id,
                userId: winnerId,
                rankNo: rankNo,
                offeredAt: nowUtc,
                expiresAt: nowUtc); // Expired immediately as it's a record of default
            
            defaultRecord.MarkAsDefaulted(nowUtc);
            _winnerOffers.Add(defaultRecord);
        }

        RaiseDomainEvent(new AuctionPaymentDefaultedEvent(
            AuctionId: $"{Id}",
            SellerId: $"{Item.SellerId}",
            DefaultedWinnerId: $"{WinnerId}",
            OccurredAt: nowUtc));

        return UnitResult.Success<Error>();
    }

    public Result<AuctionWinnerOffer, Error> OfferRunnerUp(DateTime nowUtc, TimeSpan expirationWindow)
    {
        if (Status != AuctionStatus.PaymentDefaulted)
            return AuctionErrors.Auction.InvalidState(Status.Id, "offer runner-up");

        ExpirePendingWinnerOffers(nowUtc);

        if (_winnerOffers.Any(offer => offer.IsActiveAt(nowUtc)))
            return AuctionErrors.Auction.WinnerOfferAlreadyActive;

        var rankedBids = GetRankedBids();
        var offeredUserIds = _winnerOffers
            .Where(x => x.OfferStatus != WinnerOfferStatus.Cancelled)
            .Select(x => x.UserId)
            .ToHashSet();

        var nextCandidate = rankedBids
            .Where(bid => WinnerId is null || bid.BidderId != WinnerId)
            .FirstOrDefault(bid => !offeredUserIds.Contains(bid.BidderId));

        if (nextCandidate is null)
            return AuctionErrors.Auction.NoMoreRunnerUps;

        var rankNo = rankedBids
            .Select((bid, index) => new { bid.BidderId, Rank = index + 1 })
            .First(x => x.BidderId == nextCandidate.BidderId)
            .Rank;
        var offer = AuctionWinnerOffer.Create(
            auctionId: Id,
            userId: nextCandidate.BidderId,
            rankNo: rankNo,
            offeredAt: nowUtc,
            expiresAt: nowUtc.Add(expirationWindow));

        _winnerOffers.Add(offer);
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionRunnerUpOfferedEvent(
            AuctionId: $"{Id}",
            SellerId: $"{Item.SellerId}",
            BidderId: $"{offer.UserId}",
            RankNo: rankNo,
            ExpiresAt: offer.ExpiresAt ?? nowUtc.Add(expirationWindow),
            OccurredAt: nowUtc));

        return offer;
    }

    public Result<AuctionWinnerOffer, Error> RespondToRunnerUpOffer(
        UserId bidderId,
        bool accept,
        DateTime nowUtc)
    {
        var offer = _winnerOffers
            .Where(x => x.UserId == bidderId)
            .OrderByDescending(x => x.OfferedAt)
            .FirstOrDefault();

        if (offer is null)
            return AuctionErrors.Auction.NoRunnerUp;

        if (offer.OfferStatus != WinnerOfferStatus.Pending)
            return AuctionErrors.Auction.InvalidWinnerOfferState;

        if (offer.ExpiresAt.HasValue && offer.ExpiresAt.Value < nowUtc)
        {
            offer.Expire(nowUtc);
            return AuctionErrors.Auction.WinnerOfferExpired;
        }

        UnitResult<Error> responseResult = accept
            ? offer.Accept(nowUtc)
            : offer.Decline(nowUtc);

        if (responseResult.IsFailure)
            return responseResult.Error;

        ModifiedAt = nowUtc;

        if (accept)
        {
            var rankedBid = GetRankedBids().FirstOrDefault(x => x.BidderId == bidderId);
            if (rankedBid is null)
                return AuctionErrors.Auction.NoRunnerUp;

            CancelWinnerBidsExcept(rankedBid);

            var newPrice = Pricing.WithNewBid(rankedBid.Amount.Amount, Pricing.StartingAmount);
            if (newPrice.IsFailure)
                return newPrice.Error;

            WinnerId = bidderId;
            Pricing = newPrice.Value;
            rankedBid.MarkAsWon();
            SyncAutoBidStateToWinner(bidderId, nowUtc);
            BidCount = _bids.Count(b => b.Status != Enums.BidStatus.Cancelled);
            Status = AuctionStatus.Sold;

            RaiseDomainEvent(new AuctionSoldEvent(
                AuctionId: $"{Id}",
                WinnerId: $"{WinnerId}",
                SellerId: $"{Item.SellerId}",
                FinalPrice: Pricing.CurrentAmount,
                Currency: Pricing.Currency.Id,
                TotalBids: BidCount,
                OccurredAt: nowUtc));
        }

        RaiseDomainEvent(new AuctionRunnerUpOfferRespondedEvent(
            AuctionId: $"{Id}",
            BidderId: $"{bidderId}",
            Response: accept ? "accepted" : "declined",
            OccurredAt: nowUtc));

        return offer;
    }

    public bool ExpirePendingWinnerOffers(DateTime nowUtc)
    {
        var changed = false;
        foreach (var offer in _winnerOffers.Where(x =>
                     x.OfferStatus == WinnerOfferStatus.Pending &&
                     x.ExpiresAt.HasValue &&
                     x.ExpiresAt.Value < nowUtc))
        {
            offer.Expire(nowUtc);
            changed = true;
        }

        if (changed)
            ModifiedAt = nowUtc;

        return changed;
    }

    public AuctionRelistHistory RegisterRelist(AuctionId newAuctionId, string? reason, DateTime nowUtc)
    {
        var relist = AuctionRelistHistory.Create(
            auctionId: Id,
            relistNo: _relistHistories.Count + 1,
            reason: reason,
            createdAt: nowUtc,
            newAuctionId: newAuctionId);

        _relistHistories.Add(relist);
        ModifiedAt = nowUtc;

        RaiseDomainEvent(new AuctionRelistedEvent(
            SourceAuctionId: $"{Id}",
            NewAuctionId: $"{newAuctionId}",
            SellerId: $"{Item.SellerId}",
            Reason: reason,
            OccurredAt: nowUtc));

        return relist;
    }
    
    private UnitResult<Error> EnsureNotSeller(UserId bidderId)
    {
        return bidderId == Item.SellerId ? 
            AuctionErrors.Auction.SelfBid :
            UnitResult.Success<Error>();
    }
    
    private UnitResult<Error> EnsureCanTransition(AuctionStatus target)
    {
        return !Status.CanTransitionTo(target) ?
            AuctionErrors.Auction.InvalidState(Status.Id, $"transition to {target.Id}") :
            UnitResult.Success<Error>();
    }

    private Bid? GetRecordedWinnerBid()
    {
        if (WinnerId is null)
            return null;

        return _bids
            .Where(b => b.BidderId == WinnerId &&
                        (b.Status == BidStatus.Winning || b.Status == BidStatus.Won))
            .OrderByDescending(b => b.Amount.Amount)
            .ThenBy(b => b.CreatedAt)
            .FirstOrDefault();
    }

    private void CancelWinnerBidsExcept(Bid nextWinnerBid)
    {
        foreach (var bid in _bids.Where(b =>
                     b.Id != nextWinnerBid.Id &&
                     (b.Status == BidStatus.Winning || b.Status == BidStatus.Won)))
        {
            bid.Cancel();
        }

        var previousWinnerBid = GetRecordedWinnerBid();
        if (previousWinnerBid is not null && previousWinnerBid.Id != nextWinnerBid.Id)
        {
            previousWinnerBid.Cancel();
        }
    }

    private void SyncAutoBidStateToWinner(UserId newWinnerId, DateTime nowUtc)
    {
        foreach (var ab in _autoBids)
        {
            if (ab.BidderId == newWinnerId)
            {
                if (ab.Status != AutoBidStatus.Won)
                    ab.MarkAsWon(nowUtc);
            }
            else
            {
                if (ab.Status == AutoBidStatus.Won ||
                    ab.Status == AutoBidStatus.Active ||
                    ab.Status == AutoBidStatus.Paused)
                    ab.MarkAsOutbid(nowUtc);
            }
        }
    }

    private void TerminalizeAllAutoBids(DateTime nowUtc)
    {
        foreach (var ab in _autoBids.Where(ab =>
                     ab.IsEnabled ||
                     ab.Status == AutoBidStatus.Active ||
                     ab.Status == AutoBidStatus.Paused))
        {
            ab.MarkAsOutbid(nowUtc);
        }
    }

    /// <summary>
    /// Engage an auto-bid immediately on configure/update/resume.
    /// Branches:
    ///   - User is current winner ? skip self-bid (only max/increment update on the AutoBid)
    ///   - No bids yet (BidCount == 0 or no winner) ? place opening proxy bid at GetMinimumBidAmount()
    ///   - Otherwise ? engage proxy resolution against current winner (existing behavior)
    /// Tie-break rule (same max ? earlier bidder wins) is preserved by EngageAutoBidAgainstCurrentWinner
    /// for branches that route through it, and naturally for the opening branch (only one bidder).
    /// </summary>
    private UnitResult<Error> EngageAutoBidNow(
        AutoBid autoBid,
        UserId bidderId,
        DateTime nowUtc,
        TimeSpan extensionThresholdMinutes = default,
        int maxExtensions = 0,
        TimeSpan maxDuration = default)
    {
        // Branch A: user is current winner — only update max/increment on the AutoBid, no self-bid
        var currentWinning = GetCurrentWinningBid();
        if (currentWinning is not null && currentWinning.BidderId == bidderId)
            return UnitResult.Success<Error>();

        // Branch B: no bids yet — place an opening proxy bid at the minimum amount
        if (BidCount == 0 || currentWinning is null)
        {
            var minimumRequired = GetMinimumBidAmount();
            if (!autoBid.CanBid(minimumRequired))
                return UnitResult.Success<Error>();

            // Cap opening bid at buy-now ceiling if configured ceiling exceeds it
            var openingAmount = minimumRequired.Amount;
            if (Pricing.HasBuyNowPrice && openingAmount > Pricing.BuyNowAmount!.Value)
                openingAmount = Pricing.BuyNowAmount.Value;

            var openingPrice = Money.Of(openingAmount, Pricing.Currency);
            var openingResult = PlaceAutoBidInternal(
                autoBid, openingPrice, nowUtc,
                raiseOutbidEvent: false,
                extensionThresholdMinutes: extensionThresholdMinutes,
                maxExtensions: maxExtensions,
                maxDuration: maxDuration);

            if (openingResult.IsFailure)
                return openingResult.Error;

            // Other auto-bids may now want to counter the opening bid — run proxy resolution
            return ResolveProxyBids(
                excludeBidderId: bidderId,
                nowUtc: nowUtc.AddTicks(1),
                extensionThresholdMinutes: extensionThresholdMinutes,
                maxExtensions: maxExtensions,
                maxDuration: maxDuration);
        }

        // Branch C: there is a current winner who is not this bidder — engage proxy
        return EngageAutoBidAgainstCurrentWinner(
            autoBid, bidderId, nowUtc,
            extensionThresholdMinutes, maxExtensions, maxDuration);
    }

    private UnitResult<Error> EngageAutoBidAgainstCurrentWinner(
        AutoBid autoBid,
        UserId bidderId,
        DateTime nowUtc,
        TimeSpan extensionThresholdMinutes = default,
        int maxExtensions = 0,
        TimeSpan maxDuration = default)
    {
        var currentWinning = GetCurrentWinningBid();
        if (currentWinning is null || currentWinning.BidderId == bidderId)
            return UnitResult.Success<Error>();

        // Use proxy resolution: this auto-bid vs current winner
        var minimumRequired = GetMinimumBidAmount();
        if (!autoBid.CanBid(minimumRequired))
            return UnitResult.Success<Error>();

        // Determine ceilings
        var autoBidCeiling = autoBid.Budget.MaxAmount;
        var currentWinnerCeiling = Pricing.CurrentAmount; // visible price is their minimum

        // Check if current winner also has an active auto-bid
        var winnerAutoBid = _autoBids
            .FirstOrDefault(ab => ab.BidderId == currentWinning.BidderId &&
                                  ab.IsEnabled &&
                                  ab.Status == AutoBidStatus.Active);

        if (winnerAutoBid is not null)
            currentWinnerCeiling = winnerAutoBid.Budget.MaxAmount;

        // Determine who wins the proxy resolution
        if (autoBidCeiling > currentWinnerCeiling)
        {
            // New auto-bid wins — resolved price = min(new ceiling, winner ceiling + increment)
            var resolvedAmount = Math.Min(autoBidCeiling, currentWinnerCeiling + Pricing.BidIncrementAmount);
            if (resolvedAmount < minimumRequired.Amount)
                resolvedAmount = minimumRequired.Amount;
            if (resolvedAmount > autoBidCeiling)
                resolvedAmount = autoBidCeiling;

            var resolvedPrice = Money.Of(resolvedAmount, Pricing.Currency);
            var placeResult = PlaceAutoBidInternal(autoBid, resolvedPrice, nowUtc,
                raiseOutbidEvent: true,
                extensionThresholdMinutes: extensionThresholdMinutes,
                maxExtensions: maxExtensions,
                maxDuration: maxDuration);

            if (placeResult.IsFailure)
                return placeResult.Error;

            // Mark current winner's auto-bid as outbid
            if (winnerAutoBid is not null && winnerAutoBid.Status == AutoBidStatus.Active)
                winnerAutoBid.MarkAsOutbid(nowUtc);
        }
        else if (autoBidCeiling == currentWinnerCeiling)
        {
            // Tie: current winner keeps lead (they bid first)
            // New auto-bid places at their max but still loses
            if (autoBid.Status == AutoBidStatus.Active)
                autoBid.MarkAsOutbid(nowUtc);
        }
        else
        {
            // Current winner has higher ceiling — they defend
            // Resolved price = min(winner ceiling, new auto-bid ceiling + increment)
            if (winnerAutoBid is not null)
            {
                var resolvedAmount = Math.Min(currentWinnerCeiling, autoBidCeiling + Pricing.BidIncrementAmount);
                if (resolvedAmount < minimumRequired.Amount)
                    resolvedAmount = minimumRequired.Amount;
                if (resolvedAmount > currentWinnerCeiling)
                    resolvedAmount = currentWinnerCeiling;

                var resolvedPrice = Money.Of(resolvedAmount, Pricing.Currency);
                var placeResult = PlaceAutoBidInternal(winnerAutoBid, resolvedPrice, nowUtc,
                    raiseOutbidEvent: false,
                    extensionThresholdMinutes: extensionThresholdMinutes,
                    maxExtensions: maxExtensions,
                    maxDuration: maxDuration);

                if (placeResult.IsFailure)
                    return placeResult.Error;
            }

            // New auto-bid is outbid
            if (autoBid.Status == AutoBidStatus.Active)
                autoBid.MarkAsOutbid(nowUtc);
        }

        return UnitResult.Success<Error>();
    }

    private bool IsBidEligibleParticipant(AuctionParticipant participant, DateTime nowUtc)
    {
        if (!participant.IsQualified)
            return false;

        return _deposits.Any(d => d.BidderId == participant.UserId && d.IsHeld);
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

    private Result<AuctionBuyNowReservation, Error> FindBuyNowReservation(AuctionBuyNowReservationId reservationId)
    {
        var reservation = _buyNowReservations.FirstOrDefault(x => x.Id == reservationId);

        if (reservation is null)
            return AuctionErrors.BuyNowReservation.NotFound(reservationId);

        return reservation;
    }
}
