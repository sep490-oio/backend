using System.Net;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.Grains;
using OIO.Domain.Context.AuctionContext.Grains.GrainModels;
using OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Wallets;
using OIO.Domain.Context.PaymentContext.Descriptions;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using OIO.Application.Context.AuctionContext.EventHandlers;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Infrastructure.Settings.Apps;

namespace OIO.Infrastructure.Grains;

/// <summary>
/// Orleans grain that wraps auction domain operations.
/// 
/// Key design decisions:
/// - Grain is a thin wrapper around the Auction aggregate
/// - Grain ensures single-threaded access per auction (no race conditions)
/// - Domain logic stays in the Auction aggregate (DDD)
/// - Grain loads from DB on first call, then caches in memory
/// - After domain operation, saves back to DB
/// - Domain events ? Outbox messages (via SaveChanges interceptor)
/// </summary>
public sealed class AuctionGrain : Grain, IAuctionGrain
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly ILogger<AuctionGrain> _logger;
    private readonly IRuntimeSettings _runtimeSettings;

    // In-memory cache of the auction aggregate
    private Auction? _auction;
    private bool _isLoaded;
    private IServiceScope? _loadScope;

    public AuctionGrain(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        ILogger<AuctionGrain> logger,
        IRuntimeSettings runtimeSettings)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _logger = logger;
        _runtimeSettings = runtimeSettings;
    }

    // ==================== PlaceBid ====================

    public async Task<Result<BidGrain, Error>> PlaceBidAsync(
        Guid bidderId, 
        MoneyGrain amount,
        IPAddress? ipAddress,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _clock.UtcNow;

            var (_, isFailure, auction, error) = await LoadAuctionAsync(cancellationToken);

            if (isFailure)
            {
                return error;
            }

            (_, isFailure, var amountDomain, error) = MoneyGrain.ToMoney(amount);

            if (isFailure)
            {
                return error;
            }

            // Capture previous winner for outbid detection
            var previousWinnerIdRaw = auction.GetCurrentWinningBid()?.BidderId.Value;

            // Execute bid (may trigger auto-bid cascade + extension)
            (_, isFailure, var bid, error) = auction.PlaceBid(
                bidderId: UserId.From(bidderId),
                amount: amountDomain,
                nowUtc: nowUtc,
                extensionThresholdMinutes: _runtimeSettings.Auction.ExtensionThreshold,
                maxExtensions: _runtimeSettings.Auction.MaxExtensionsPerAuction,
                maxDuration: _runtimeSettings.Auction.MaxDuration,
                ipAddress: ipAddress);

            if (isFailure)
            {
                DiscardLoadedAuction();
                return error;
            }

            // Capture state before save (scope disposal clears everything)
            var auctionId = auction.Id.Value;
            var currentPrice = auction.Pricing.CurrentAmount;
            var minNextBid = auction.GetMinimumBidAmount().Amount;
            var totalBids = auction.BidCount;
            var currency = auction.Pricing.Currency.Id;
            var bidId = bid.Id.Value;
            var bidAmount = amountDomain.Amount;
            var isAutoBid = bid.IsAutoBid;
            var bidTime = bid.CreatedAt;

            // Detect auto-bid counter-bid produced by ResolveProxyBids inside PlaceBid.
            // When a third party has an active autobid with a higher max than this manual bid,
            // the domain creates an additional Bid entity (IsAutoBid=true) that becomes the
            // current winner. We must publish a second realtime event for that bid so the
            // frontend bid history and outbid notifications stay in sync.
            var winningBid = auction.GetCurrentWinningBid();
            var hasAutoBidCascade = winningBid is not null && winningBid.Id.Value != bidId;

            Guid autoBidCounterId = default;
            Guid autoBidCounterBidderId = default;
            decimal autoBidCounterAmount = default;
            DateTime autoBidCounterTime = default;

            if (hasAutoBidCascade)
            {
                autoBidCounterId = winningBid!.Id.Value;
                autoBidCounterBidderId = winningBid.BidderId.Value;
                autoBidCounterAmount = winningBid.Amount.Amount;
                autoBidCounterTime = winningBid.CreatedAt;
            }

            await SaveAsync(auction, cancellationToken);

            // Publish realtime events immediately after commit
            await PublishRealtimeAsync(auctionId, async (publisher, dbContext) =>
            {
                var displayName = await ResolveBidderDisplayNameAsync(dbContext, bidderId, cancellationToken);
                var bidTimestamp = new DateTimeOffset(DateTime.SpecifyKind(bidTime, DateTimeKind.Utc));

                // For the manual bid event, when the autobid cascade overrode it the "current price"
                // shown to outbid recipients should still be the manual bid's own amount (the autobid
                // gets its own event below). Without this split the FE would receive a single event
                // where currentPrice > lastBid.amount which breaks price-history and outbid messaging.
                var manualCurrentPrice = hasAutoBidCascade ? bidAmount : currentPrice;
                var manualMinNextBid = hasAutoBidCascade ? bidAmount : minNextBid;

                // BidPlaced + AuctionStateChanged for the MANUAL bid
                await publisher.PublishBidPlacedAsync(
                    auctionId,
                    new BidNotification(
                        AuctionId: auctionId,
                        BidId: bidId,
                        BidderId: bidderId,
                        BidderDisplayName: displayName,
                        Amount: bidAmount,
                        CurrentPrice: manualCurrentPrice,
                        MinimumNextBid: manualMinNextBid,
                        TotalBids: hasAutoBidCascade ? totalBids - 1 : totalBids,
                        IsAutoBid: isAutoBid,
                        Timestamp: bidTimestamp),
                    new AuctionStateSyncOptions(
                        LastBid: new AuctionStateLastBidInfo(
                            BidId: bidId,
                            BidderId: bidderId,
                            BidderDisplayName: displayName,
                            Amount: bidAmount,
                            IsAutoBid: isAutoBid,
                            Timestamp: bidTimestamp),
                        NewPriceHistoryPoint: new AuctionStatePriceHistoryPoint(
                            Price: manualCurrentPrice,
                            Type: "bid",
                            BidId: bidId,
                            BidderDisplayName: displayName,
                            RecordedAt: bidTimestamp)),
                    cancellationToken);

                // Outbid notification to the previous winner (before any of this action)
                if (previousWinnerIdRaw is not null && previousWinnerIdRaw.Value != bidderId)
                {
                    await publisher.PublishOutbidAsync(
                        previousWinnerIdRaw.Value,
                        new OutbidNotification(
                            AuctionId: auctionId,
                            NewHighAmount: manualCurrentPrice,
                            MinimumNextBid: manualMinNextBid,
                            NewHighBidderDisplayName: displayName),
                        cancellationToken);
                }

                // Second event pair for the auto-bid counter-bid (if one won proxy resolution)
                if (hasAutoBidCascade)
                {
                    var autoBidderDisplayName = await ResolveBidderDisplayNameAsync(
                        dbContext, autoBidCounterBidderId, cancellationToken);
                    var autoBidTimestamp = new DateTimeOffset(
                        DateTime.SpecifyKind(autoBidCounterTime, DateTimeKind.Utc));

                    await publisher.PublishBidPlacedAsync(
                        auctionId,
                        new BidNotification(
                            AuctionId: auctionId,
                            BidId: autoBidCounterId,
                            BidderId: autoBidCounterBidderId,
                            BidderDisplayName: autoBidderDisplayName,
                            Amount: autoBidCounterAmount,
                            CurrentPrice: currentPrice,
                            MinimumNextBid: minNextBid,
                            TotalBids: totalBids,
                            IsAutoBid: true,
                            Timestamp: autoBidTimestamp),
                        new AuctionStateSyncOptions(
                            LastBid: new AuctionStateLastBidInfo(
                                BidId: autoBidCounterId,
                                BidderId: autoBidCounterBidderId,
                                BidderDisplayName: autoBidderDisplayName,
                                Amount: autoBidCounterAmount,
                                IsAutoBid: true,
                                Timestamp: autoBidTimestamp),
                            NewPriceHistoryPoint: new AuctionStatePriceHistoryPoint(
                                Price: currentPrice,
                                Type: "bid",
                                BidId: autoBidCounterId,
                                BidderDisplayName: autoBidderDisplayName,
                                RecordedAt: autoBidTimestamp)),
                        cancellationToken);

                    // The manual bidder was briefly winning, then immediately outbid by the autobid.
                    if (autoBidCounterBidderId != bidderId)
                    {
                        await publisher.PublishOutbidAsync(
                            bidderId,
                            new OutbidNotification(
                                AuctionId: auctionId,
                                NewHighAmount: currentPrice,
                                MinimumNextBid: minNextBid,
                                NewHighBidderDisplayName: autoBidderDisplayName),
                            cancellationToken);
                    }
                }
            });

            // Publish auto-bid state changes for all affected bidders in the cascade
            await PublishAllAutoBidStatesAsync(auctionId, cancellationToken);

            // Publish position changes for manual bidder + previous winner (+ auto-bidder when cascade ran)
            await PublishPositionAsync(auctionId, bidderId, cancellationToken);
            if (previousWinnerIdRaw is not null && previousWinnerIdRaw.Value != bidderId)
            {
                await PublishPositionAsync(auctionId, previousWinnerIdRaw.Value, cancellationToken);
            }
            if (hasAutoBidCascade && autoBidCounterBidderId != bidderId
                && (previousWinnerIdRaw is null || autoBidCounterBidderId != previousWinnerIdRaw.Value))
            {
                await PublishPositionAsync(auctionId, autoBidCounterBidderId, cancellationToken);
            }

            return BidGrain.From(bid);
        }
        catch (Exception ex)
        {
            DiscardLoadedAuction();
            _logger.LogError(ex,
                "Unexpected error placing bid on auction {AuctionId}", this.GetGrainId());
            throw;
        }
    }

    public async Task<Result<AuctionBuyNowReservationGrain, Error>> InitiateBuyNowReservationAsync(
        Guid bidderId,
        TimeSpan reservationWindow,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _clock.UtcNow;
            var (_, isFailure, auction, error) = await LoadAuctionAsync(cancellationToken);

            if (isFailure)
                return error;

            (_, isFailure, var reservation, error) = auction.InitiateBuyNowReservation(
                UserId.From(bidderId),
                nowUtc,
                reservationWindow);

            if (isFailure)
            {
                DiscardLoadedAuction();
                return error;
            }

            var rtAuctionId = auction.Id.Value;
            await SaveAsync(auction, cancellationToken);
            await PublishRealtimeAsync(rtAuctionId, (pub, _) => pub.PublishStateChangedAsync(rtAuctionId, ct: cancellationToken));
            return AuctionBuyNowReservationGrain.From(reservation);
        }
        catch (Exception ex)
        {
            DiscardLoadedAuction();
            _logger.LogError(ex,
                "Unexpected error initiating buy-now reservation on auction {AuctionId}",
                this.GetGrainId());
            throw;
        }
    }

    public async Task<Result<AuctionBuyNowReservationGrain, Error>> AttachBuyNowPaymentAsync(
        Guid reservationId,
        Guid paymentTransactionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _clock.UtcNow;
            var (_, isFailure, auction, error) = await LoadAuctionAsync(cancellationToken);

            if (isFailure)
                return error;

            (_, isFailure, var reservation, error) = auction.AttachBuyNowPayment(
                AuctionBuyNowReservationId.From(reservationId),
                TransactionId.From(paymentTransactionId),
                nowUtc);

            if (isFailure)
            {
                DiscardLoadedAuction();
                return error;
            }

            var rtAuctionId = auction.Id.Value;
            await SaveAsync(auction, cancellationToken);
            await PublishRealtimeAsync(rtAuctionId, (pub, _) => pub.PublishStateChangedAsync(rtAuctionId, ct: cancellationToken));
            return AuctionBuyNowReservationGrain.From(reservation);
        }
        catch (Exception ex)
        {
            DiscardLoadedAuction();
            _logger.LogError(ex,
                "Unexpected error attaching buy-now payment on auction {AuctionId}",
                this.GetGrainId());
            throw;
        }
    }

    public async Task<UnitResult<Error>> FailBuyNowReservationAsync(
        Guid reservationId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _clock.UtcNow;
            var (_, isFailure, auction, error) = await LoadAuctionAsync(cancellationToken);

            if (isFailure)
                return error;

            var result = auction.FailBuyNowReservation(
                AuctionBuyNowReservationId.From(reservationId),
                reason,
                nowUtc);

            if (result.IsFailure)
            {
                DiscardLoadedAuction();
                return result.Error;
            }

            var rtAuctionId = auction.Id.Value;
            await SaveAsync(auction, cancellationToken);
            await PublishRealtimeAsync(rtAuctionId, (pub, _) => pub.PublishStateChangedAsync(rtAuctionId, ct: cancellationToken));
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            DiscardLoadedAuction();
            _logger.LogError(ex,
                "Unexpected error failing buy-now reservation on auction {AuctionId}",
                this.GetGrainId());
            throw;
        }
    }

    // ==================== ConfigureAutoBid ====================

    public async Task<Result<AutoBidGrain, Error>> ConfigureAutoBidAsync(
        Guid bidderId, 
        MoneyGrain maxAmount,
        MoneyGrain? incrementAmount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _clock.UtcNow;
            
            var (_, isFailure, auction, error) = await LoadAuctionAsync(cancellationToken);

            if (isFailure)
            {
                return error;
            }
            
            (_, isFailure, var maxAmountDomain, error)  = MoneyGrain.ToMoney(maxAmount);
            
            if (isFailure)
            {
                return error;
            }
            
            Money? incrementAmountDomain = null;
            
            if (incrementAmount is not null)
            {
                (_, isFailure, incrementAmountDomain, error)  = MoneyGrain.ToMoney(incrementAmount.Value);
            
                if (isFailure)
                {
                    return error;
                }
            }

            var bidderUserId = UserId.From(bidderId);
            var validationResult = auction.ValidateAutoBidConfiguration(
                bidderUserId,
                maxAmountDomain,
                nowUtc,
                incrementAmountDomain);

            if (validationResult.IsFailure)
            {
                DiscardLoadedAuction();
                return validationResult.Error;
            }

            // Capture previous max before domain update (for wallet holdDelta calculation)
            var existingAutoBid = auction.AutoBids
                .FirstOrDefault(ab => ab.BidderId == bidderUserId);
            var previousMaxAmount = existingAutoBid?.Budget.MaxAmount ?? 0m;
            var holdDelta = maxAmountDomain.Amount - previousMaxAmount;

            // Snapshot bid state before — auto-bid engagement may place opening / counter bids
            var bidIdsBefore = auction.Bids.Select(b => b.Id).ToHashSet();
            var previousWinnerIdRaw = auction.GetCurrentWinningBid()?.BidderId.Value;

            // Perform domain operation first (no DB side-effects yet)
            (_, isFailure, var autoBid, error) = auction.ConfigureAutoBid(
                bidderUserId,
                maxAmountDomain,
                nowUtc,
                incrementAmountDomain);

            if (isFailure)
            {
                DiscardLoadedAuction();
                return error;
            }

            // Track actual held amount on the auto-bid entity so fund release uses the real held value
            autoBid.SetHeldAmount(maxAmountDomain.Amount);

            var rtAuctionId = auction.Id.Value;

            // Detect bids placed by the engagement (opening bid and/or counter-bids from other auto-bids)
            var newBids = auction.Bids
                .Where(b => !bidIdsBefore.Contains(b.Id))
                .OrderBy(b => b.CreatedAt)
                .Select(b => new BidPublishSnapshot(
                    BidId: b.Id.Value,
                    BidderId: b.BidderId.Value,
                    Amount: b.Amount.Amount,
                    IsAutoBid: b.IsAutoBid,
                    CreatedAt: b.CreatedAt))
                .ToList();
            var currentPriceAfter = auction.Pricing.CurrentAmount;
            var minNextBidAfter = auction.GetMinimumBidAmount().Amount;
            var totalBidsAfter = auction.BidCount;
            var winnerAfter = auction.GetCurrentWinningBid();
            var winnerBidIdAfter = winnerAfter?.Id.Value;
            var winnerBidderIdAfter = winnerAfter?.BidderId.Value;

            if (holdDelta != 0m)
            {
                // Wallet hold/unhold + auction save in the SAME scope that loaded the auction.
                // This ensures EF correctly tracks new vs existing entities (no Update() needed).
                if (_loadScope is null)
                    throw new InvalidOperationException("Cannot save auction without an active load scope.");

                var dbContext = _loadScope.ServiceProvider.GetRequiredService<IDbContext>();
                var unitOfWork = _loadScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var wallet = await dbContext.Set<Wallet>()
                    .FirstOrDefaultAsync(w => w.UserId == bidderUserId, cancellationToken);

                if (wallet is null)
                {
                    DiscardLoadedAuction();
                    _logger.LogError(
                        "Wallet not found for auto-bid reservation on auction {AuctionId}. UserId={UserId}",
                        this.GetGrainId(),
                        bidderId);
                    return Error.NotFound("Wallet.NotFound", "User wallet not found.");
                }

                UnitResult<Error> holdResult = holdDelta > 0
                    ? wallet.Hold(holdDelta, null, LedgerDescriptions.AutoBidReservation(this.GetGrainId().ToString()), nowUtc)
                    : wallet.Unhold(Math.Abs(holdDelta), null, LedgerDescriptions.AutoBidReservationAdjusted(this.GetGrainId().ToString()), nowUtc);

                if (holdResult.IsFailure)
                {
                    DiscardLoadedAuction();
                    _logger.LogWarning(
                        "Wallet hold/unhold failed for auto-bid on auction {AuctionId}: {Error}",
                        this.GetGrainId(),
                        holdResult.Error.Message);
                    return Error.Conflict("Wallet.HoldFailed", holdResult.Error.Message);
                }

                // Save wallet + auction atomically — same scope tracks both
                await unitOfWork.SaveChangesAsync(cancellationToken);
                DiscardLoadedAuction();
            }
            else
            {
                await SaveAsync(auction, cancellationToken);
            }

            if (newBids.Count > 0)
            {
                // Publish a BidPlaced for each newly created bid (opening + any cascading counters)
                await PublishRealtimeAsync(rtAuctionId, async (publisher, dbContext) =>
                {
                    for (var i = 0; i < newBids.Count; i++)
                    {
                        var nb = newBids[i];
                        var isLast = i == newBids.Count - 1;
                        var displayName = await ResolveBidderDisplayNameAsync(dbContext, nb.BidderId, cancellationToken);
                        var ts = new DateTimeOffset(DateTime.SpecifyKind(nb.CreatedAt, DateTimeKind.Utc));

                        // For non-final bids in the cascade, currentPrice is the bid's own amount; the final bid
                        // carries the resolved auction state (matches the PlaceBidAsync convention).
                        var pubCurrentPrice = isLast ? currentPriceAfter : nb.Amount;
                        var pubMinNextBid = isLast ? minNextBidAfter : nb.Amount;
                        var pubTotalBids = isLast ? totalBidsAfter : (totalBidsAfter - (newBids.Count - 1 - i));

                        await publisher.PublishBidPlacedAsync(
                            rtAuctionId,
                            new BidNotification(
                                AuctionId: rtAuctionId,
                                BidId: nb.BidId,
                                BidderId: nb.BidderId,
                                BidderDisplayName: displayName,
                                Amount: nb.Amount,
                                CurrentPrice: pubCurrentPrice,
                                MinimumNextBid: pubMinNextBid,
                                TotalBids: pubTotalBids,
                                IsAutoBid: nb.IsAutoBid,
                                Timestamp: ts),
                            new AuctionStateSyncOptions(
                                LastBid: new AuctionStateLastBidInfo(
                                    BidId: nb.BidId,
                                    BidderId: nb.BidderId,
                                    BidderDisplayName: displayName,
                                    Amount: nb.Amount,
                                    IsAutoBid: nb.IsAutoBid,
                                    Timestamp: ts),
                                NewPriceHistoryPoint: new AuctionStatePriceHistoryPoint(
                                    Price: pubCurrentPrice,
                                    Type: "bid",
                                    BidId: nb.BidId,
                                    BidderDisplayName: displayName,
                                    RecordedAt: ts)),
                            cancellationToken);
                    }

                    // Outbid notification for the previous winner (if any) — once, against the final winner
                    if (previousWinnerIdRaw is not null
                        && winnerBidderIdAfter is not null
                        && previousWinnerIdRaw.Value != winnerBidderIdAfter.Value)
                    {
                        var winnerName = await ResolveBidderDisplayNameAsync(dbContext, winnerBidderIdAfter.Value, cancellationToken);
                        await publisher.PublishOutbidAsync(
                            previousWinnerIdRaw.Value,
                            new OutbidNotification(
                                AuctionId: rtAuctionId,
                                NewHighAmount: currentPriceAfter,
                                MinimumNextBid: minNextBidAfter,
                                NewHighBidderDisplayName: winnerName),
                            cancellationToken);
                    }
                });

                // Auto-bid state for every affected bidder (covers configurer + outbid bidders in cascade)
                await PublishAllAutoBidStatesAsync(rtAuctionId, cancellationToken);

                // Position changes for: configurer, previous winner (if not configurer), final winner (if different)
                await PublishPositionAsync(rtAuctionId, bidderId, cancellationToken);
                if (previousWinnerIdRaw is not null && previousWinnerIdRaw.Value != bidderId)
                    await PublishPositionAsync(rtAuctionId, previousWinnerIdRaw.Value, cancellationToken);
                if (winnerBidderIdAfter is not null
                    && winnerBidderIdAfter.Value != bidderId
                    && (previousWinnerIdRaw is null || winnerBidderIdAfter.Value != previousWinnerIdRaw.Value))
                {
                    await PublishPositionAsync(rtAuctionId, winnerBidderIdAfter.Value, cancellationToken);
                }
            }
            else
            {
                await PublishRealtimeAsync(rtAuctionId, (pub, _) => pub.PublishStateChangedAsync(rtAuctionId, ct: cancellationToken));
                await PublishAutoBidStateAsync(rtAuctionId, bidderId, cancellationToken);
                await PublishPositionAsync(rtAuctionId, bidderId, cancellationToken);
            }

            var result = AutoBidGrain.From(autoBid);
            result = result with { PreviousMaxAmount = previousMaxAmount };
            return result;
        }
        catch (Exception ex)
        {
            DiscardLoadedAuction();
            _logger.LogError(ex,
                "Unexpected error configuring auto-bid on auction {AuctionId}",
                this.GetGrainId());
            throw;
        }
    }

    public async Task<UnitResult<Error>> PauseAutoBidAsync(
        Guid bidderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _clock.UtcNow;
            var (_, isFailure, auction, error) = await LoadAuctionAsync(cancellationToken);

            if (isFailure)
                return error;

            var result = auction.PauseAutoBid(UserId.From(bidderId), nowUtc);
            if (result.IsFailure)
            {
                DiscardLoadedAuction();
                return result.Error;
            }

            var rtAuctionId = auction.Id.Value;
            await SaveAsync(auction, cancellationToken);
            await PublishRealtimeAsync(rtAuctionId, (pub, _) => pub.PublishStateChangedAsync(rtAuctionId, ct: cancellationToken));
            await PublishAutoBidStateAsync(rtAuctionId, bidderId, cancellationToken);
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            DiscardLoadedAuction();
            _logger.LogError(ex,
                "Unexpected error pausing auto-bid on auction {AuctionId}",
                this.GetGrainId());
            throw;
        }
    }

    public async Task<UnitResult<Error>> ResumeAutoBidAsync(
        Guid bidderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _clock.UtcNow;
            var (_, isFailure, auction, error) = await LoadAuctionAsync(cancellationToken);

            if (isFailure)
                return error;

            // Snapshot bid state before — Resume engages auto-bid which may place new bids
            var bidIdsBefore = auction.Bids.Select(b => b.Id).ToHashSet();
            var previousWinnerIdRaw = auction.GetCurrentWinningBid()?.BidderId.Value;

            var result = auction.ResumeAutoBid(UserId.From(bidderId), nowUtc);
            if (result.IsFailure)
            {
                DiscardLoadedAuction();
                return result.Error;
            }

            var rtAuctionId = auction.Id.Value;

            // Detect bids placed by the engagement
            var newBids = auction.Bids
                .Where(b => !bidIdsBefore.Contains(b.Id))
                .OrderBy(b => b.CreatedAt)
                .Select(b => new BidPublishSnapshot(
                    BidId: b.Id.Value,
                    BidderId: b.BidderId.Value,
                    Amount: b.Amount.Amount,
                    IsAutoBid: b.IsAutoBid,
                    CreatedAt: b.CreatedAt))
                .ToList();
            var currentPriceAfter = auction.Pricing.CurrentAmount;
            var minNextBidAfter = auction.GetMinimumBidAmount().Amount;
            var totalBidsAfter = auction.BidCount;
            var winnerAfter = auction.GetCurrentWinningBid();
            var winnerBidderIdAfter = winnerAfter?.BidderId.Value;

            await SaveAsync(auction, cancellationToken);

            if (newBids.Count > 0)
            {
                await PublishRealtimeAsync(rtAuctionId, async (publisher, dbContext) =>
                {
                    for (var i = 0; i < newBids.Count; i++)
                    {
                        var nb = newBids[i];
                        var isLast = i == newBids.Count - 1;
                        var displayName = await ResolveBidderDisplayNameAsync(dbContext, nb.BidderId, cancellationToken);
                        var ts = new DateTimeOffset(DateTime.SpecifyKind(nb.CreatedAt, DateTimeKind.Utc));

                        var pubCurrentPrice = isLast ? currentPriceAfter : nb.Amount;
                        var pubMinNextBid = isLast ? minNextBidAfter : nb.Amount;
                        var pubTotalBids = isLast ? totalBidsAfter : (totalBidsAfter - (newBids.Count - 1 - i));

                        await publisher.PublishBidPlacedAsync(
                            rtAuctionId,
                            new BidNotification(
                                AuctionId: rtAuctionId,
                                BidId: nb.BidId,
                                BidderId: nb.BidderId,
                                BidderDisplayName: displayName,
                                Amount: nb.Amount,
                                CurrentPrice: pubCurrentPrice,
                                MinimumNextBid: pubMinNextBid,
                                TotalBids: pubTotalBids,
                                IsAutoBid: nb.IsAutoBid,
                                Timestamp: ts),
                            new AuctionStateSyncOptions(
                                LastBid: new AuctionStateLastBidInfo(
                                    BidId: nb.BidId,
                                    BidderId: nb.BidderId,
                                    BidderDisplayName: displayName,
                                    Amount: nb.Amount,
                                    IsAutoBid: nb.IsAutoBid,
                                    Timestamp: ts),
                                NewPriceHistoryPoint: new AuctionStatePriceHistoryPoint(
                                    Price: pubCurrentPrice,
                                    Type: "bid",
                                    BidId: nb.BidId,
                                    BidderDisplayName: displayName,
                                    RecordedAt: ts)),
                            cancellationToken);
                    }

                    if (previousWinnerIdRaw is not null
                        && winnerBidderIdAfter is not null
                        && previousWinnerIdRaw.Value != winnerBidderIdAfter.Value)
                    {
                        var winnerName = await ResolveBidderDisplayNameAsync(dbContext, winnerBidderIdAfter.Value, cancellationToken);
                        await publisher.PublishOutbidAsync(
                            previousWinnerIdRaw.Value,
                            new OutbidNotification(
                                AuctionId: rtAuctionId,
                                NewHighAmount: currentPriceAfter,
                                MinimumNextBid: minNextBidAfter,
                                NewHighBidderDisplayName: winnerName),
                            cancellationToken);
                    }
                });

                await PublishAllAutoBidStatesAsync(rtAuctionId, cancellationToken);

                await PublishPositionAsync(rtAuctionId, bidderId, cancellationToken);
                if (previousWinnerIdRaw is not null && previousWinnerIdRaw.Value != bidderId)
                    await PublishPositionAsync(rtAuctionId, previousWinnerIdRaw.Value, cancellationToken);
                if (winnerBidderIdAfter is not null
                    && winnerBidderIdAfter.Value != bidderId
                    && (previousWinnerIdRaw is null || winnerBidderIdAfter.Value != previousWinnerIdRaw.Value))
                {
                    await PublishPositionAsync(rtAuctionId, winnerBidderIdAfter.Value, cancellationToken);
                }
            }
            else
            {
                await PublishRealtimeAsync(rtAuctionId, (pub, _) => pub.PublishStateChangedAsync(rtAuctionId, ct: cancellationToken));
                await PublishAutoBidStateAsync(rtAuctionId, bidderId, cancellationToken);
                await PublishPositionAsync(rtAuctionId, bidderId, cancellationToken);
            }

            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            DiscardLoadedAuction();
            _logger.LogError(ex,
                "Unexpected error resuming auto-bid on auction {AuctionId}",
                this.GetGrainId());
            throw;
        }
    }

    public async Task<UnitResult<Error>> CancelAutoBidAsync(
        Guid bidderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _clock.UtcNow;
            var bidderUserId = UserId.From(bidderId);

            var (_, isFailure, auction, error) = await LoadAuctionAsync(cancellationToken);

            if (isFailure)
                return error;

            var (_, cancelFailure, autoBid, cancelError) = auction.CancelAutoBid(bidderUserId, nowUtc);
            if (cancelFailure)
            {
                DiscardLoadedAuction();
                return cancelError;
            }

            // Release wallet hold immediately on user-initiated cancel
            var holdAmount = autoBid.HeldAmount;
            var rtAuctionId = auction.Id.Value;
            if (holdAmount > 0m)
            {
                // Wallet unhold + auction cancel in the SAME scope that loaded the auction.
                if (_loadScope is null)
                    throw new InvalidOperationException("Cannot save auction without an active load scope.");

                var dbContext = _loadScope.ServiceProvider.GetRequiredService<IDbContext>();
                var unitOfWork = _loadScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var wallet = await dbContext.Set<Wallet>()
                    .FirstOrDefaultAsync(w => w.UserId == bidderUserId, cancellationToken);

                if (wallet is null)
                {
                    DiscardLoadedAuction();
                    _logger.LogError(
                        "Wallet not found when cancelling auto-bid on auction {AuctionId}. UserId={UserId}",
                        this.GetGrainId(),
                        bidderId);
                    return Error.NotFound("Wallet.NotFound", "User wallet not found.");
                }

                var unholdResult = wallet.Unhold(
                    holdAmount,
                    transactionId: null,
                    description: LedgerDescriptions.AutoBidCancelled(this.GetGrainId().ToString()),
                    nowUtc: nowUtc);

                if (unholdResult.IsFailure)
                {
                    DiscardLoadedAuction();
                    _logger.LogWarning(
                        "Failed to release auto-bid reservation on cancel for auction {AuctionId}: {Error}",
                        this.GetGrainId(),
                        unholdResult.Error.Message);
                    return Error.Conflict("Wallet.UnholdFailed", unholdResult.Error.Message);
                }

                // Reset tracked held amount after successful release
                autoBid.SetHeldAmount(0m);

                // Save wallet + auction atomically — same scope tracks both
                await unitOfWork.SaveChangesAsync(cancellationToken);
                DiscardLoadedAuction();
            }
            else
            {
                await SaveAsync(auction, cancellationToken);
            }
            await PublishRealtimeAsync(rtAuctionId, (pub, _) => pub.PublishStateChangedAsync(rtAuctionId, ct: cancellationToken));
            await PublishAutoBidStateAsync(rtAuctionId, bidderId, cancellationToken);
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            DiscardLoadedAuction();
            _logger.LogError(ex,
                "Unexpected error cancelling auto-bid on auction {AuctionId}",
                this.GetGrainId());
            throw;
        }
    }

    public async Task<UnitResult<Error>> EndAuctionAsync(
        Guid? revealerId,
        IReadOnlyCollection<RevealedSealedBidAmountGrain>? revealedSealedBids,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _clock.UtcNow;
            var (_, isFailure, auction, error) = await LoadAuctionAsync(cancellationToken);

            if (isFailure)
                return error;

            if (auction.Status != AuctionStatus.Active)
                return UnitResult.Success<Error>();

            // Defer ending if a buy-now reservation is still PendingPayment —
            // even if ExpiresAt <= now (the ExpireBuyNowReservationsJob hasn't
            // processed it yet). The expire job applies ApplyBuyNowCompensation
            // which extends EndTime. Ending here would race against that compensation.
            // Uses IsPendingPayment (not IsActive) to cover the window between
            // expiry and ExpireBuyNowReservationsJob processing — same guard as
            // CloseQualificationJob.
            if (auction.BuyNowReservations.Any(r => r.IsPendingPayment))
                return UnitResult.Success<Error>();

            var endResult = auction.End(nowUtc);
            if (endResult.IsFailure)
            {
                DiscardLoadedAuction();
                return endResult.Error;
            }

            if (auction.AuctionType == AuctionType.Sealed)
            {
                if (revealedSealedBids is null)
                {
                    DiscardLoadedAuction();
                    return Error.Validation(
                        "SealedBids",
                        "Auction.SealedRevealRequired",
                        "Revealed sealed bids are required to end a sealed auction.");
                }

                var revealedAmounts = new List<RevealedSealedBidAmount>(revealedSealedBids.Count);
                foreach (var revealedSealedBid in revealedSealedBids)
                {
                    var moneyResult = MoneyGrain.ToMoney(revealedSealedBid.Amount);
                    if (moneyResult.IsFailure)
                    {
                        DiscardLoadedAuction();
                        return moneyResult.Error;
                    }

                    revealedAmounts.Add(new RevealedSealedBidAmount(
                        SealedBidId.From(revealedSealedBid.SealedBidId),
                        moneyResult.Value));
                }

                var revealResult = auction.RevealAllSealedBids(
                    revealedAmounts,
                    revealerId.HasValue ? UserId.From(revealerId.Value) : null,
                    nowUtc);

                if (revealResult.IsFailure)
                {
                    DiscardLoadedAuction();
                    return revealResult.Error;
                }
            }

            var resolveResult = auction.Resolve(nowUtc);
            if (resolveResult.IsFailure)
            {
                DiscardLoadedAuction();
                return resolveResult.Error;
            }

            var rtAuctionId = auction.Id.Value;
            await SaveAsync(auction, cancellationToken);
            await PublishRealtimeAsync(rtAuctionId, (pub, _) => pub.PublishStateChangedAsync(rtAuctionId, ct: cancellationToken));
            // Publish terminal auto-bid states (won/outbid) for all bidders
            await PublishAllAutoBidStatesAsync(rtAuctionId, cancellationToken);
            // Publish terminal position (won/lost) for all bidders
            await PublishAllPositionsAsync(rtAuctionId, cancellationToken);
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            DiscardLoadedAuction();
            _logger.LogError(ex,
                "Unexpected error ending auction {AuctionId}",
                this.GetGrainId());
            throw;
        }
    }

    // ==================== Snapshot (read) ====================

    public async Task<Result<AuctionSnapshotGrain, Error>> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var (_, isFailure, auction, error) = await LoadAuctionAsync(cancellationToken);

        if (isFailure)
        {
            return error;
        }

        return new AuctionSnapshotGrain(
            AuctionId: auction.Id.Value,
            CurrentPrice: auction.Pricing.CurrentAmount,
            MinimumNextBid: auction.GetMinimumBidAmount().Amount,
            BidCount: auction.BidCount,
            Status: auction.Status.Id,
            EndTime: auction.Info?.EndTime ?? DateTime.MinValue,
            WinnerId: auction.WinnerId?.Value,
            BuyNowPrice: auction.Pricing.HasBuyNowPrice ? auction.Pricing.BuyNowAmount : null);
    }

    // ==================== Internal Helpers ====================

    /// <summary>
    /// Load auction from DB on first access, then use cached version.
    /// Invalidate cache after save to stay consistent.
    /// </summary>
    public async Task<UnitResult<Error>> ForceCancelAuctionAsync(string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _clock.UtcNow;
            var (_, isFailure, auction, error) = await LoadAuctionAsync(cancellationToken);

            if (isFailure) return error;

            var result = auction.CancelAuction($"[ADMIN] {reason}", nowUtc, isAdminOverride: true);
            if (result.IsFailure) return result.Error;

            await SaveAsync(auction, cancellationToken);
            await PublishRealtimeAsync(auction.Id.Value, (pub, _) => pub.PublishStateChangedAsync(auction.Id.Value, ct: cancellationToken));
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error forcing cancel auction {AuctionId}", this.GetGrainId());
            return Error.Unexpected("AuctionGrain.ForceCancelAuctionFailed", "Unexpected error.");
        }
    }

    public async Task<UnitResult<Error>> TerminateAuctionAsync(string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _clock.UtcNow;
            var (_, isFailure, auction, error) = await LoadAuctionAsync(cancellationToken);

            if (isFailure) return error;

            var result = auction.Terminate($"[ADMIN] {reason}", nowUtc);
            if (result.IsFailure) return result.Error;

            await SaveAsync(auction, cancellationToken);
            await PublishRealtimeAsync(auction.Id.Value, (pub, _) => pub.PublishStateChangedAsync(auction.Id.Value, ct: cancellationToken));
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error terminating auction {AuctionId}", this.GetGrainId());
            return Error.Unexpected("AuctionGrain.TerminateAuctionFailed", "Unexpected error.");
        }
    }

    public async Task<UnitResult<Error>> ForceStartQualificationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _clock.UtcNow;
            var (_, isFailure, auction, error) = await LoadAuctionAsync(cancellationToken);

            if (isFailure) return error;

            var result = auction.ForceStartQualification(nowUtc);
            if (result.IsFailure) return result.Error;

            await SaveAsync(auction, cancellationToken);
            await PublishRealtimeAsync(auction.Id.Value, (pub, _) => pub.PublishStateChangedAsync(auction.Id.Value, ct: cancellationToken));
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error forcing start qualification {AuctionId}", this.GetGrainId());
            return Error.Unexpected("AuctionGrain.ForceStartQualificationFailed", "Unexpected error.");
        }
    }

    public async Task<UnitResult<Error>> ForceStartBiddingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var nowUtc = _clock.UtcNow;
            var (_, isFailure, auction, error) = await LoadAuctionAsync(cancellationToken);

            if (isFailure) return error;

            var result = auction.ForceStartBidding(nowUtc);
            if (result.IsFailure) return result.Error;

            var bidIdsBefore = auction.Bids.Select(b => b.Id).ToHashSet();

            auction.EngageAllAutoBidsOnStart(nowUtc);

            var newBids = auction.Bids
                .Where(b => !bidIdsBefore.Contains(b.Id))
                .OrderBy(b => b.CreatedAt)
                .Select(b => new BidPublishSnapshot(
                    BidId: b.Id.Value,
                    BidderId: b.BidderId.Value,
                    Amount: b.Amount.Amount,
                    IsAutoBid: b.IsAutoBid,
                    CreatedAt: b.CreatedAt))
                .ToList();
            var currentPriceAfter = auction.Pricing.CurrentAmount;
            var minNextBidAfter = auction.GetMinimumBidAmount().Amount;
            var totalBidsAfter = auction.BidCount;

            await SaveAsync(auction, cancellationToken);
            
            if (newBids.Count > 0)
            {
                await PublishRealtimeAsync(auction.Id.Value, async (publisher, dbContext) =>
                {
                    for (var i = 0; i < newBids.Count; i++)
                    {
                        var nb = newBids[i];
                        var isLast = i == newBids.Count - 1;
                        var displayName = await ResolveBidderDisplayNameAsync(dbContext, nb.BidderId, cancellationToken);
                        var ts = new DateTimeOffset(DateTime.SpecifyKind(nb.CreatedAt, DateTimeKind.Utc));

                        var pubCurrentPrice = isLast ? currentPriceAfter : nb.Amount;
                        var pubMinNextBid = isLast ? minNextBidAfter : nb.Amount;
                        var pubTotalBids = isLast ? totalBidsAfter : (totalBidsAfter - (newBids.Count - 1 - i));

                        await publisher.PublishBidPlacedAsync(
                            auction.Id.Value,
                            new BidNotification(
                                AuctionId: auction.Id.Value,
                                BidId: nb.BidId,
                                BidderId: nb.BidderId,
                                BidderDisplayName: displayName,
                                Amount: nb.Amount,
                                CurrentPrice: pubCurrentPrice,
                                MinimumNextBid: pubMinNextBid,
                                TotalBids: pubTotalBids,
                                IsAutoBid: nb.IsAutoBid,
                                Timestamp: ts),
                            new AuctionStateSyncOptions(
                                LastBid: new AuctionStateLastBidInfo(
                                    BidId: nb.BidId,
                                    BidderId: nb.BidderId,
                                    BidderDisplayName: displayName,
                                    Amount: nb.Amount,
                                    IsAutoBid: nb.IsAutoBid,
                                    Timestamp: ts),
                                NewPriceHistoryPoint: new AuctionStatePriceHistoryPoint(
                                    Price: pubCurrentPrice,
                                    Type: "bid",
                                    BidId: nb.BidId,
                                    BidderDisplayName: displayName,
                                    RecordedAt: ts)),
                            cancellationToken);
                    }
                });

                await PublishAllAutoBidStatesAsync(auction.Id.Value, cancellationToken);
                await PublishRealtimeAsync(auction.Id.Value, (pub, _) => pub.PublishStateChangedAsync(auction.Id.Value, ct: cancellationToken));
            }
            else
            {
                await PublishRealtimeAsync(auction.Id.Value, (pub, _) => pub.PublishStateChangedAsync(auction.Id.Value, ct: cancellationToken));
            }

            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error forcing start bidding {AuctionId}", this.GetGrainId());
            return Error.Unexpected("AuctionGrain.ForceStartBiddingFailed", "Unexpected error.");
        }
    }

    private async Task<Result<Auction, Error>> LoadAuctionAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoaded && _auction is not null)
            return _auction;

        var auctionId = this.GetPrimaryKey();

        // Keep scope alive so the DbContext tracks entities for SaveAsync.
        // Disposed in DiscardLoadedAuction() or SaveAsync().
        _loadScope?.Dispose();
        _loadScope = _scopeFactory.CreateScope();
        var dbContext = _loadScope.ServiceProvider.GetRequiredService<IDbContext>();

        _auction = await dbContext.GetByIdAsync<Auction, AuctionId>(
            AuctionId.From(auctionId),
            query => query
                .Include(a => a.Item)
                .Include(a => a.Bids.OrderByDescending(b => b.CreatedAt))
                .Include(a => a.AutoBids)
                .Include(a => a.Deposits)
                .Include(a => a.Participants)
                .Include(a => a.SealedBids)
                .Include(a => a.PriceHistories.OrderByDescending(ph => ph.CreatedAt))
                .Include(a => a.BuyNowReservations)
                .AsSplitQuery(),
            cancellationToken);

        if (_auction is null)
        {
            _loadScope.Dispose();
            _loadScope = null;
            return AuctionErrors.Auction.NotFound(AuctionId.From(auctionId));
        }

        _isLoaded = true;

        return _auction;
    }

    /// <summary>
    /// Save auction to DB and invalidate grain cache.
    /// Domain events in auction → Outbox messages (via interceptor).
    /// Creates a fresh DI scope so the DbContext is short-lived.
    /// Retries on concurrency conflict by reloading entries.
    /// </summary>
    private async Task SaveAsync(Auction auction, CancellationToken cancellationToken = default)
    {
        if (_loadScope is null)
            throw new InvalidOperationException("Cannot save auction without an active load scope.");

        var unitOfWork = _loadScope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Dispose the scope and clear cached state
        _loadScope.Dispose();
        _loadScope = null;
        DiscardLoadedAuction();
    }

    /// <summary>
    /// Publishes realtime events immediately after commit.
    /// Creates a fresh DI scope since the load scope is disposed after SaveAsync.
    /// </summary>
    private async Task PublishRealtimeAsync(Guid auctionId, Func<IAuctionRealtimePublisher, IDbContext, Task> action)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IAuctionRealtimePublisher>();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
            await action(publisher, dbContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish realtime events for auction {AuctionId}", auctionId);
        }
    }

    private async Task PublishAutoBidStateAsync(Guid auctionId, Guid bidderId, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IAutoBidRealtimePublisher>();
            await publisher.PublishAsync(auctionId, bidderId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish AutoBidStateChanged for auction {AuctionId}, bidder {BidderId}",
                auctionId, bidderId);
        }
    }

    private async Task PublishAllAutoBidStatesAsync(Guid auctionId, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IAutoBidRealtimePublisher>();
            await publisher.PublishAllForAuctionAsync(auctionId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish AutoBidStateChanged for all bidders on auction {AuctionId}",
                auctionId);
        }
    }

    private async Task PublishPositionAsync(Guid auctionId, Guid bidderId, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IAuctionPositionPublisher>();
            await publisher.PublishAsync(auctionId, bidderId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish AuctionPositionChanged for auction {AuctionId}, bidder {BidderId}",
                auctionId, bidderId);
        }
    }

    private async Task PublishAllPositionsAsync(Guid auctionId, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IAuctionPositionPublisher>();
            await publisher.PublishForAllBiddersAsync(auctionId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish AuctionPositionChanged for all bidders on auction {AuctionId}",
                auctionId);
        }
    }

    private static async Task<string> ResolveBidderDisplayNameAsync(
        IDbContext dbContext, Guid bidderId, CancellationToken ct)
    {
        var user = await dbContext.GetByIdAsync<User, UserId>(
            UserId.From(bidderId),
            queryBuilder: q => q.AsNoTracking().Include(u => u.Profile),
            cancellationToken: ct);
        return AuctionNotificationDisplayNames.Resolve(user);
    }

    public Task InvalidateCacheAsync()
    {
        DiscardLoadedAuction();
        return Task.CompletedTask;
    }

    private void DiscardLoadedAuction()
    {
        _isLoaded = false;
        _auction = null;
        _loadScope?.Dispose();
        _loadScope = null;
    }

    /// <summary>
    /// Snapshot of a newly created bid captured before SaveAsync, so realtime publishing
    /// can run after the load scope is disposed.
    /// </summary>
    private readonly record struct BidPublishSnapshot(
        Guid BidId,
        Guid BidderId,
        decimal Amount,
        bool IsAutoBid,
        DateTime CreatedAt);
}

