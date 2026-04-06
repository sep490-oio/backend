using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.Services;

public sealed record AuctionStateSyncOptions(
    AuctionStateLastBidInfo? LastBid = null,
    AuctionStatePriceHistoryPoint? NewPriceHistoryPoint = null);

public sealed class AuctionStateSyncService
{
    private readonly IDbContext _dbContext;
    private readonly IAuctionNotificationService _notificationService;
    private readonly IRuntimeSettings _runtimeSettings;
    private readonly ILogger<AuctionStateSyncService> _logger;

    public AuctionStateSyncService(
        IDbContext dbContext,
        IAuctionNotificationService notificationService,
        IRuntimeSettings runtimeSettings,
        ILogger<AuctionStateSyncService> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _runtimeSettings = runtimeSettings;
        _logger = logger;
    }

    public async Task PublishAsync(
        Guid auctionId,
        AuctionStateSyncOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entityId = AuctionId.From(auctionId);
            var nowUtc = DateTime.UtcNow;

            var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
                entityId,
                queryBuilder: query => query
                    .AsNoTracking()
                    .Include(a => a.BuyNowReservations),
                cancellationToken: cancellationToken);

            if (auction is null)
            {
                _logger.LogWarning(
                    "Skipped AuctionStateChanged broadcast because auction {AuctionId} could not be loaded.",
                    auctionId);
                return;
            }

            var activeReservation = auction.GetActiveBuyNowReservation(nowUtc);
            var endTime = ToUtcOffset(auction.Info?.EndTime ?? auction.ModifiedAt ?? auction.CreatedAt);
            var versionTimestamp = ToUtcOffset(auction.ModifiedAt ?? auction.CreatedAt);

            await _notificationService.NotifyAuctionStateChangedAsync(
                auctionId,
                new AuctionStateChangedNotification(
                    AuctionId: auctionId,
                    Status: auction.Status.Id,
                    CurrentPrice: auction.Pricing.CurrentAmount,
                    MinimumNextBid: CanAcceptMoreBids(auction) ? auction.GetMinimumBidAmount().Amount : 0,
                    Currency: auction.Pricing.Currency.Id,
                    BidCount: auction.BidCount,
                    EndTime: endTime,
                    WinnerId: auction.WinnerId?.Value,
                    IsBuyNowReserved: activeReservation is not null,
                    BuyNowReservedUntil: activeReservation is not null
                        ? ToUtcOffset(activeReservation.ExpiresAt)
                        : null,
                    AutoExtend: auction.Info?.AutoExtend ?? false,
                    ExtensionMinutes: auction.Info?.ExtensionMinutes ?? 0,
                    ExtensionCount: auction.Info?.ExtensionCount ?? 0,
                    IsEndingSoon: auction.Info is not null &&
                                  auction.IsEndingSoon(nowUtc, _runtimeSettings.Auction.ExtensionThreshold),
                    LastBid: options?.LastBid,
                    NewPriceHistoryPoint: options?.NewPriceHistoryPoint,
                    ServerTimestamp: DateTimeOffset.UtcNow,
                    VersionTimestamp: versionTimestamp),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to broadcast AuctionStateChanged for auction {AuctionId}. " +
                "Granular events (BidPlaced, etc.) were already sent — clients will still receive those.",
                auctionId);
        }
    }

    private static bool CanAcceptMoreBids(Auction auction)
    {
        return string.Equals(auction.Status.Id, "active", StringComparison.OrdinalIgnoreCase)
            || string.Equals(auction.Status.Id, "scheduled", StringComparison.OrdinalIgnoreCase);
    }

    private static DateTimeOffset ToUtcOffset(DateTime value)
    {
        var utcValue = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };

        return new DateTimeOffset(utcValue);
    }
}
