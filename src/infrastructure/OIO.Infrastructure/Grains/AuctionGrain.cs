using System.Net;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
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
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
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
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<AuctionGrain> _logger;
    private readonly IRuntimeSettings _runtimeSettings;

    // In-memory cache of the auction aggregate
    private Auction? _auction;
    private bool _isLoaded;

    public AuctionGrain(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<AuctionGrain> logger,
        IRuntimeSettings runtimeSettings)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
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

            (_, isFailure, var amountDomain, error)  = MoneyGrain.ToMoney(amount);
            
            if (isFailure)
            {
                return error;
            }
            
            // The aggregate executes the full live-bidding cascade, including
            // counter auto-bids and any resulting domain events, before returning.
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
            await SaveAsync(auction, cancellationToken);

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

            await SaveAsync(auction, cancellationToken);
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

            await SaveAsync(auction, cancellationToken);
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

            await SaveAsync(auction, cancellationToken);
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

            if (holdDelta != 0m)
            {
                var wallet = await _dbContext.Set<Wallet>()
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
                    ? wallet.Hold(holdDelta, null, $"Auto-bid reservation for Auction {this.GetGrainId()}", nowUtc)
                    : wallet.Unhold(Math.Abs(holdDelta), null, $"Auto-bid reservation adjusted for Auction {this.GetGrainId()}", nowUtc);

                if (holdResult.IsFailure)
                {
                    DiscardLoadedAuction();
                    _logger.LogWarning(
                        "Wallet hold/unhold failed for auto-bid on auction {AuctionId}: {Error}",
                        this.GetGrainId(),
                        holdResult.Error.Message);
                    return Error.Conflict("Wallet.HoldFailed", holdResult.Error.Message);
                }
            }

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

            await SaveAsync(auction, cancellationToken);

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

            await SaveAsync(auction, cancellationToken);
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

            var result = auction.ResumeAutoBid(UserId.From(bidderId), nowUtc);
            if (result.IsFailure)
            {
                DiscardLoadedAuction();
                return result.Error;
            }

            await SaveAsync(auction, cancellationToken);
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

            if (auction.GetActiveBuyNowReservation(nowUtc) is not null)
                return UnitResult.Success<Error>();

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

            var endResult = auction.End(nowUtc);
            if (endResult.IsFailure)
            {
                DiscardLoadedAuction();
                return endResult.Error;
            }

            var resolveResult = auction.Resolve(nowUtc);
            if (resolveResult.IsFailure)
            {
                DiscardLoadedAuction();
                return resolveResult.Error;
            }

            await SaveAsync(auction, cancellationToken);
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
            WinnerId: auction.WinnerId?.Value);
    }

    // ==================== Internal Helpers ====================

    /// <summary>
    /// Load auction from DB on first access, then use cached version.
    /// Invalidate cache after save to stay consistent.
    /// </summary>
    private async Task<Result<Auction, Error>> LoadAuctionAsync(CancellationToken cancellationToken = default)
    {
        if (_isLoaded && _auction is not null)
            return _auction;

        var auctionId = this.GetPrimaryKey();

        _auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
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
            return AuctionErrors.Auction.NotFound(AuctionId.From(auctionId));
        }
        
        _isLoaded = true;

        return _auction;
    }

    /// <summary>
    /// Save auction to DB and invalidate grain cache.
    /// Domain events in auction ? Outbox messages (via interceptor).
    /// </summary>
    private async Task SaveAsync(Auction auction, CancellationToken cancellationToken = default)
    {
         _dbContext.Update(auction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        DiscardLoadedAuction();
    }

    public Task InvalidateCacheAsync()
    {
        DiscardLoadedAuction();
        return Task.CompletedTask;
    }

    private void DiscardLoadedAuction()
    {
        if (_dbContext is DbContext efContext)
            efContext.ChangeTracker.Clear();

        _isLoaded = false;
        _auction = null;
    }
}

