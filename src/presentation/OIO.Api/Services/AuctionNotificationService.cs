using Microsoft.AspNetCore.SignalR;
using OIO.Api.Hubs;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;

namespace OIO.Api.Services;

internal sealed class AuctionNotificationService : IAuctionNotificationService
{
    private readonly IHubContext<AuctionHub, IAuctionHubClient> _hubContext;

    public AuctionNotificationService(
        IHubContext<AuctionHub, IAuctionHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyBidPlacedAsync(
        Guid auctionId, BidNotification notification, CancellationToken ct)
    {
        await _hubContext
            .Clients
            .Group(AuctionHub.AuctionGroupName(auctionId))
            .BidPlaced(notification);
    }

    public async Task NotifyOutbidAsync(
        Guid outbidUserId, OutbidNotification notification, CancellationToken ct)
    {
        await _hubContext
            .Clients
            .Group(AuctionHub.UserGroupName(outbidUserId))
            .Outbid(notification);
    }

    public async Task NotifyBuyNowExecutedAsync(
        Guid auctionId, BuyNowNotification notification, CancellationToken ct)
    {
        await _hubContext
            .Clients
            .Group(AuctionHub.AuctionGroupName(auctionId))
            .BuyNowExecuted(notification);
    }

    public async Task NotifyAuctionStartedAsync(
        Guid auctionId, AuctionStartedNotification notification, CancellationToken ct)
    {
        await _hubContext
            .Clients
            .Group(AuctionHub.AuctionGroupName(auctionId))
            .AuctionStarted(notification);
    }

    public async Task NotifyAuctionEndedAsync(
        Guid auctionId, AuctionEndedNotification notification, CancellationToken ct)
    {
        // Broadcast to auction group
        await _hubContext
            .Clients
            .Group(AuctionHub.AuctionGroupName(auctionId))
            .AuctionEnded(notification);

        // Notify winner directly
        if (notification.WinnerId.HasValue)
        {
            await _hubContext
                .Clients
                .Group(AuctionHub.UserGroupName(notification.WinnerId.Value))
                .AuctionEnded(notification with { WinnerDisplayName = "You" });
        }
    }

    public async Task NotifyAuctionExtendedAsync(
        Guid auctionId, AuctionExtendedNotification notification, CancellationToken ct)
    {
        await _hubContext
            .Clients
            .Group(AuctionHub.AuctionGroupName(auctionId))
            .AuctionExtended(notification);
    }

    public async Task NotifyAuctionCancelledAsync(
        Guid auctionId, AuctionCancelledNotification notification, CancellationToken ct)
    {
        await _hubContext
            .Clients
            .Group(AuctionHub.AuctionGroupName(auctionId))
            .AuctionCancelled(notification);
    }
}