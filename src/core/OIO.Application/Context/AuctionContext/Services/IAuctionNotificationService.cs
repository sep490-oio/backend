using OIO.Application.Context.AuctionContext.Hubs;

namespace OIO.Application.Context.AuctionContext.Services;

/// <summary>
/// Abstraction for sending real-time notifications to auction participants.
/// Application layer depends on this interface.
/// Presentation layer implements it using SignalR IHubContext.
/// </summary>
public interface IAuctionNotificationService
{
    /// <summary>
    /// Broadcast bid placed to everyone watching the auction.
    /// </summary>
    Task NotifyBidPlacedAsync(
        Guid auctionId,
        BidNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify a specific user that they've been outbid.
    /// </summary>
    Task NotifyOutbidAsync(
        Guid outbidUserId,
        OutbidNotification notification,
        CancellationToken cancellationToken = default);

    Task NotifyBuyNowReservedAsync(
        Guid auctionId,
        BuyNowReservedNotification notification,
        CancellationToken cancellationToken = default);

    Task NotifyBuyNowReservationReleasedAsync(
        Guid auctionId,
        BuyNowReservationReleasedNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast buy now executed to auction group.
    /// </summary>
    Task NotifyBuyNowExecutedAsync(
        Guid auctionId,
        BuyNowNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast auction started to auction group.
    /// </summary>
    Task NotifyAuctionStartedAsync(
        Guid auctionId,
        AuctionStartedNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast auction ended to auction group + notify winner directly.
    /// </summary>
    Task NotifyAuctionEndedAsync(
        Guid auctionId,
        AuctionEndedNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast auction time extended.
    /// </summary>
    Task NotifyAuctionExtendedAsync(
        Guid auctionId,
        AuctionExtendedNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast auction cancelled.
    /// </summary>
    Task NotifyAuctionCancelledAsync(
        Guid auctionId,
        AuctionCancelledNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast question asked to item group.
    /// </summary>
    Task NotifyQuestionAskedAsync(
        Guid itemId,
        ItemQuestionNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Broadcast question answered to item group.
    /// </summary>
    Task NotifyQuestionAnsweredAsync(
        Guid itemId,
        ItemQuestionNotification notification,
        CancellationToken cancellationToken = default);
}
