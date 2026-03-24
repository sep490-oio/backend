using Microsoft.AspNetCore.Mvc;

namespace OIO.Application.Context.AuctionContext.Hubs;

/// <summary>
/// Methods that the server can invoke on connected clients.
/// </summary>
public interface IAuctionHubClient
{
    // ==================== Bid Events ====================
    Task BidPlaced(BidNotification notification);
    Task Outbid(OutbidNotification notification);
    Task BuyNowReserved(BuyNowReservedNotification notification);
    Task BuyNowReservationReleased(BuyNowReservationReleasedNotification notification);
    Task BuyNowExecuted(BuyNowNotification notification);

    // ==================== Auction Lifecycle ====================
    Task AuctionStarted(AuctionStartedNotification notification);
    Task AuctionEnded(AuctionEndedNotification notification);
    Task AuctionExtended(AuctionExtendedNotification notification);
    Task AuctionCancelled(AuctionCancelledNotification notification);

    // ==================== Price Update ====================
    Task PriceUpdated(PriceUpdateNotification notification);

    // ==================== Q&A ====================
    Task QuestionAsked(ItemQuestionNotification notification);
    Task QuestionAnswered(ItemQuestionNotification notification);

    // ==================== Error ====================
    Task Error(ErrorNotification notification);
}

// ==================== Notification Records ====================

public sealed record BidNotification(
    Guid AuctionId,
    Guid BidId,
    Guid BidderId,
    string BidderDisplayName,
    decimal Amount,
    decimal CurrentPrice,
    decimal MinimumNextBid,
    int TotalBids,
    bool IsAutoBid,
    DateTimeOffset Timestamp);

public sealed record OutbidNotification(
    Guid AuctionId,
    decimal NewHighAmount,
    decimal MinimumNextBid,
    string NewHighBidderDisplayName);

public sealed record BuyNowNotification(
    Guid AuctionId,
    Guid BuyerId,
    decimal Price);

public sealed record BuyNowReservedNotification(
    Guid AuctionId,
    Guid ReservationId,
    Guid BuyerId,
    decimal BuyNowPrice,
    decimal DepositAppliedAmount,
    decimal AmountDue,
    DateTimeOffset ExpiresAt);

public sealed record BuyNowReservationReleasedNotification(
    Guid AuctionId,
    Guid ReservationId,
    Guid BuyerId,
    string Reason,
    DateTimeOffset ReleasedAt);

public sealed record AuctionStartedNotification(
    Guid AuctionId,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime);

public sealed record AuctionEndedNotification(
    Guid AuctionId,
    Guid? WinnerId,
    string? WinnerDisplayName,
    decimal FinalPrice,
    int TotalBids,
    bool ReserveMet);

public sealed record AuctionExtendedNotification(
    Guid AuctionId,
    DateTimeOffset NewEndTime,
    int ExtensionMinutes);

public sealed record AuctionCancelledNotification(
    Guid AuctionId,
    string Reason);

public sealed record PriceUpdateNotification(
    Guid AuctionId,
    decimal CurrentPrice,
    decimal MinimumNextBid,
    int TotalBids,
    TimeSpan RemainingTime);

public sealed record ItemQuestionNotification(
    Guid ItemId,
    Guid QuestionId,
    Guid AskerId,
    string AskerDisplayName,
    string Question,
    string? Answer,
    bool IsPublic,
    DateTime CreatedAt);

public sealed record ErrorNotification(
    string Code,
    string Message,
    Dictionary<string, string[]>? Errors);
