namespace OIO.Application.Context.UserContext.Hubs;

// Notification DTOs
public sealed record WalletUpdatedNotification(
    decimal AvailableBalance,
    decimal PendingBalance,
    decimal TotalBalance,
    string TransactionType,
    decimal Amount,
    string Currency);

public sealed record BidStatusChangedNotification(
    Guid AuctionId,
    Guid BidId,
    string NewStatus,
    decimal CurrentPrice,
    string Currency);

public sealed record AuctionOutcomeNotification(
    Guid AuctionId,
    string AuctionTitle,
    string Outcome,
    decimal FinalPrice,
    string Currency);

public sealed record OrderStatusChangedNotification(
    Guid OrderId,
    string OrderNumber,
    string NewStatus,
    string PreviousStatus);

public interface IUserHubClient
{
    Task WalletUpdated(WalletUpdatedNotification notification);
    Task BidStatusChanged(BidStatusChangedNotification notification);
    Task AuctionOutcomeForBidder(AuctionOutcomeNotification notification);
    Task OrderStatusChanged(OrderStatusChangedNotification notification);
}
