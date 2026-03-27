using Microsoft.AspNetCore.SignalR;
using OIO.Api.Hubs;
using OIO.Application.Context.UserContext.Hubs;

namespace OIO.Api.Services;

public sealed class UserNotificationService(IHubContext<UserHub, IUserHubClient> hubContext)
    : IUserNotificationService
{
    public Task NotifyWalletUpdatedAsync(Guid userId, WalletUpdatedNotification notification)
        => hubContext.Clients.Group($"user:{userId}").WalletUpdated(notification);

    public Task NotifyBidStatusChangedAsync(Guid userId, BidStatusChangedNotification notification)
        => hubContext.Clients.Group($"user:{userId}").BidStatusChanged(notification);

    public Task NotifyAuctionOutcomeAsync(Guid userId, AuctionOutcomeNotification notification)
        => hubContext.Clients.Group($"user:{userId}").AuctionOutcomeForBidder(notification);

    public Task NotifyOrderStatusChangedAsync(Guid userId, OrderStatusChangedNotification notification)
        => hubContext.Clients.Group($"user:{userId}").OrderStatusChanged(notification);
}
