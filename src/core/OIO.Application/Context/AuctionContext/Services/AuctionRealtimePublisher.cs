using Microsoft.Extensions.Logging;
using OIO.Application.Context.AuctionContext.Hubs;

namespace OIO.Application.Context.AuctionContext.Services;

public sealed class AuctionRealtimePublisher : IAuctionRealtimePublisher
{
    private readonly IAuctionNotificationService _notificationService;
    private readonly AuctionStateSyncService _syncService;
    private readonly ILogger<AuctionRealtimePublisher> _logger;

    public AuctionRealtimePublisher(
        IAuctionNotificationService notificationService,
        AuctionStateSyncService syncService,
        ILogger<AuctionRealtimePublisher> logger)
    {
        _notificationService = notificationService;
        _syncService = syncService;
        _logger = logger;
    }

    public async Task PublishStateChangedAsync(Guid auctionId, AuctionStateSyncOptions? options = null, CancellationToken ct = default)
    {
        try
        {
            await _syncService.PublishAsync(auctionId, options, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish AuctionStateChanged for {AuctionId}", auctionId);
        }
    }

    public async Task PublishBidPlacedAsync(Guid auctionId, BidNotification notification, AuctionStateSyncOptions? syncOptions = null, CancellationToken ct = default)
    {
        try
        {
            await _notificationService.NotifyBidPlacedAsync(auctionId, notification, ct);
            await _syncService.PublishAsync(auctionId, syncOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish BidPlaced+StateChanged for {AuctionId}", auctionId);
        }
    }

    public async Task PublishOutbidAsync(Guid outbidUserId, OutbidNotification notification, CancellationToken ct = default)
    {
        try
        {
            await _notificationService.NotifyOutbidAsync(outbidUserId, notification, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish Outbid for user {UserId}", outbidUserId);
        }
    }

    public async Task PublishAuctionStartedAsync(Guid auctionId, AuctionStartedNotification notification, CancellationToken ct = default)
    {
        try
        {
            await _notificationService.NotifyAuctionStartedAsync(auctionId, notification, ct);
            await _syncService.PublishAsync(auctionId, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish AuctionStarted+StateChanged for {AuctionId}", auctionId);
        }
    }

    public async Task PublishAuctionEndedAsync(Guid auctionId, AuctionEndedNotification notification, CancellationToken ct = default)
    {
        try
        {
            await _notificationService.NotifyAuctionEndedAsync(auctionId, notification, ct);
            await _syncService.PublishAsync(auctionId, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish AuctionEnded+StateChanged for {AuctionId}", auctionId);
        }
    }

    public async Task PublishAuctionExtendedAsync(Guid auctionId, AuctionExtendedNotification notification, CancellationToken ct = default)
    {
        try
        {
            await _notificationService.NotifyAuctionExtendedAsync(auctionId, notification, ct);
            await _syncService.PublishAsync(auctionId, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish AuctionExtended+StateChanged for {AuctionId}", auctionId);
        }
    }

    public async Task PublishAuctionCancelledAsync(Guid auctionId, AuctionCancelledNotification notification, CancellationToken ct = default)
    {
        try
        {
            await _notificationService.NotifyAuctionCancelledAsync(auctionId, notification, ct);
            await _syncService.PublishAsync(auctionId, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish AuctionCancelled+StateChanged for {AuctionId}", auctionId);
        }
    }

    public async Task PublishBuyNowReservedAsync(Guid auctionId, BuyNowReservedNotification notification, CancellationToken ct = default)
    {
        try
        {
            await _notificationService.NotifyBuyNowReservedAsync(auctionId, notification, ct);
            await _syncService.PublishAsync(auctionId, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish BuyNowReserved+StateChanged for {AuctionId}", auctionId);
        }
    }

    public async Task PublishBuyNowReservationReleasedAsync(Guid auctionId, BuyNowReservationReleasedNotification notification, CancellationToken ct = default)
    {
        try
        {
            await _notificationService.NotifyBuyNowReservationReleasedAsync(auctionId, notification, ct);
            await _syncService.PublishAsync(auctionId, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish BuyNowReservationReleased+StateChanged for {AuctionId}", auctionId);
        }
    }

    public async Task PublishBuyNowExecutedAsync(Guid auctionId, BuyNowNotification notification, CancellationToken ct = default)
    {
        try
        {
            await _notificationService.NotifyBuyNowExecutedAsync(auctionId, notification, ct);
            await _syncService.PublishAsync(auctionId, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish BuyNowExecuted+StateChanged for {AuctionId}", auctionId);
        }
    }
}
