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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly ILogger<AuctionGrain> _logger;
    private readonly IRuntimeSettings _runtimeSettings;

    // In-memory cache of the auction aggregate
    private Auction? _auction;
    private bool _isLoaded;

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

            if (holdDelta != 0m)
            {
                // Wallet hold/unhold + auction save in a SINGLE scope/transaction
                // to prevent split-brain (wallet held but no auto-bid, or vice versa)
                using var atomicScope = _scopeFactory.CreateScope();
                var dbContext = atomicScope.ServiceProvider.GetRequiredService<IDbContext>();
                var unitOfWork = atomicScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

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

                // Save wallet + auction atomically in one SaveChangesAsync call
                dbContext.Update(auction);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                DiscardLoadedAuction();
            }
            else
            {
                await SaveAsync(auction, cancellationToken);
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
            if (holdAmount > 0m)
            {
                // Wallet unhold + auction cancel save in a SINGLE scope/transaction
                // to prevent split-brain (wallet unheld but auto-bid still active)
                using var atomicScope = _scopeFactory.CreateScope();
                var dbContext = atomicScope.ServiceProvider.GetRequiredService<IDbContext>();
                var unitOfWork = atomicScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

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
                    description: $"Auto-bid cancelled by user for auction {this.GetGrainId()}",
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

                // Save wallet + auction atomically in one SaveChangesAsync call
                dbContext.Update(auction);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                DiscardLoadedAuction();
            }
            else
            {
                await SaveAsync(auction, cancellationToken);
            }
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

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();

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
            return AuctionErrors.Auction.NotFound(AuctionId.From(auctionId));
        }

        _isLoaded = true;

        return _auction;
    }

    /// <summary>
    /// Save auction to DB and invalidate grain cache.
    /// Domain events in auction → Outbox messages (via interceptor).
    /// Creates a fresh DI scope so the DbContext is short-lived.
    /// </summary>
    private async Task SaveAsync(Auction auction, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        dbContext.Update(auction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        DiscardLoadedAuction();
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
    }
}

