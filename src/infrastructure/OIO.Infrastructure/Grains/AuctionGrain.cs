using System.Net;
using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.Grains;
using OIO.Domain.Context.AuctionContext.Grains.GrainModels;
using OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using OIO.Infrastructure.Settings;
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
/// - Domain events → Outbox messages (via SaveChanges interceptor)
/// </summary>
public sealed class AuctionGrain : Grain, IAuctionGrain
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<AuctionGrain> _logger;
    private readonly IAppConfigs _appConfigs;

    // In-memory cache of the auction aggregate
    private Auction? _auction;
    private bool _isLoaded;

    public AuctionGrain(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<AuctionGrain> logger,
        IAppConfigs appConfigs)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
        _appConfigs = appConfigs;
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
                extensionThresholdMinutes: await _appConfigs.Auctions.GetExtensionThresholdMinutesAsync(cancellationToken),
                maxExtensions: await _appConfigs.Auctions.GetMaxExtensionsPerAuctionAsync(cancellationToken),
                maxDuration: await _appConfigs.Auctions.GetMaxDurationAsync(cancellationToken),
                ipAddress: ipAddress);
            
            if (isFailure)
            {
                return error;
            }
            await SaveAsync(auction, cancellationToken);

            return BidGrain.From(bid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error placing bid on auction {AuctionId}", this.GetGrainId());
            throw;
        }
    }

    // ==================== BuyNow ====================

    public async Task<Result<BidGrain, Error>> ExecuteBuyNowAsync(
        Guid bidderId,
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

            (_, isFailure, var bid, error) = auction.ExecuteBuyNow(UserId.From(bidderId), nowUtc, ipAddress);

            if (isFailure)
            {
                return error;
            }
            
            await SaveAsync(auction, cancellationToken);

            return BidGrain.From(bid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error executing buy now on auction {AuctionId}",
                this.GetGrainId());
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
                return error;

            await SaveAsync(auction, cancellationToken);
            return AuctionBuyNowReservationGrain.From(reservation);
        }
        catch (Exception ex)
        {
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
                return error;

            await SaveAsync(auction, cancellationToken);
            return AuctionBuyNowReservationGrain.From(reservation);
        }
        catch (Exception ex)
        {
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
                return result.Error;

            await SaveAsync(auction, cancellationToken);
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
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

            (_, isFailure, var autoBid, error) = auction.ConfigureAutoBid(UserId.From(bidderId), maxAmountDomain, nowUtc, incrementAmountDomain);

            if (isFailure)
            {
                return error;
            }
            
            await SaveAsync(auction, cancellationToken);

            return AutoBidGrain.From(autoBid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error configuring auto-bid on auction {AuctionId}",
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
    /// Domain events in auction → Outbox messages (via interceptor).
    /// </summary>
    private async Task SaveAsync(Auction auction, CancellationToken cancellationToken = default)
    {
         _dbContext.Update(auction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _isLoaded = false;
        _auction = null;
    }
}
