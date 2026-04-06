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
    Task AutoBidStateChanged(AutoBidStateChangedNotification notification);
    Task AuctionPositionChanged(AuctionPositionChangedNotification notification);
}

public sealed record AutoBidStateChangedNotification(
    Guid AuctionId,
    Guid BidderId,
    bool IsEnabled,
    decimal MaxAmount,
    decimal CurrentAmount,
    decimal RemainingBudget,
    decimal? IncrementAmount,
    string Status,
    string Currency,
    int TotalAutoBids,
    DateTimeOffset? LastAutoBidAt,
    string? StopReason,
    DateTimeOffset? StoppedAt,
    DateTimeOffset? LastValidationAt,
    DateTimeOffset ServerTimestamp);

public sealed record AuctionPositionChangedNotification(
    Guid AuctionId,
    string Position,
    bool IsCurrentWinner,
    decimal CurrentPrice,
    decimal MinimumNextBid,
    string Currency,
    Guid? LatestBidId,
    decimal? LatestBidAmount,
    string? LatestBidStatus,
    DateTimeOffset Timestamp);
