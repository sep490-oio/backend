using OIO.Application.Context.AuctionContext.Hubs;

namespace OIO.Application.Context.AuctionContext.Services;

/// <summary>
/// Single entry point for immediate realtime auction notifications.
/// Publishes both semantic events (for toasts/banners) and AuctionStateChanged (for state sync).
/// Called directly after commit — does NOT depend on outbox/Quartz.
/// </summary>
public interface IAuctionRealtimePublisher
{
    /// <summary>State-only sync (no semantic event). Used for mutations without a specific semantic event.</summary>
    Task PublishStateChangedAsync(Guid auctionId, AuctionStateSyncOptions? options = null, CancellationToken ct = default);

    /// <summary>BidPlaced semantic event + AuctionStateChanged.</summary>
    Task PublishBidPlacedAsync(Guid auctionId, BidNotification notification, AuctionStateSyncOptions? syncOptions = null, CancellationToken ct = default);

    /// <summary>Outbid semantic event (targeted to outbid user). No state sync (already sent with BidPlaced).</summary>
    Task PublishOutbidAsync(Guid outbidUserId, OutbidNotification notification, CancellationToken ct = default);

    /// <summary>AuctionStarted semantic event + AuctionStateChanged.</summary>
    Task PublishAuctionStartedAsync(Guid auctionId, AuctionStartedNotification notification, CancellationToken ct = default);

    /// <summary>AuctionEnded semantic event + AuctionStateChanged.</summary>
    Task PublishAuctionEndedAsync(Guid auctionId, AuctionEndedNotification notification, CancellationToken ct = default);

    /// <summary>AuctionExtended semantic event + AuctionStateChanged.</summary>
    Task PublishAuctionExtendedAsync(Guid auctionId, AuctionExtendedNotification notification, CancellationToken ct = default);

    /// <summary>AuctionCancelled semantic event + AuctionStateChanged.</summary>
    Task PublishAuctionCancelledAsync(Guid auctionId, AuctionCancelledNotification notification, CancellationToken ct = default);

    /// <summary>BuyNowReserved semantic event + AuctionStateChanged.</summary>
    Task PublishBuyNowReservedAsync(Guid auctionId, BuyNowReservedNotification notification, CancellationToken ct = default);

    /// <summary>BuyNowReservationReleased semantic event + AuctionStateChanged.</summary>
    Task PublishBuyNowReservationReleasedAsync(Guid auctionId, BuyNowReservationReleasedNotification notification, CancellationToken ct = default);

    /// <summary>BuyNowExecuted semantic event + AuctionStateChanged.</summary>
    Task PublishBuyNowExecutedAsync(Guid auctionId, BuyNowNotification notification, CancellationToken ct = default);
}
