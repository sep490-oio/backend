namespace OIO.Application.Context.UserContext.Hubs;

/// <summary>
/// Abstraction for pushing user-scoped real-time notifications via UserHub.
/// Implemented in Api layer by UserNotificationService.
/// </summary>
public interface IUserNotificationService
{
    Task NotifyWalletUpdatedAsync(Guid userId, WalletUpdatedNotification notification);
    Task NotifyBidStatusChangedAsync(Guid userId, BidStatusChangedNotification notification);
    Task NotifyAuctionOutcomeAsync(Guid userId, AuctionOutcomeNotification notification);
    Task NotifyOrderStatusChangedAsync(Guid userId, OrderStatusChangedNotification notification);
}
