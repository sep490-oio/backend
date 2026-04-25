using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

public sealed record AuctionCreatedEvent(
    string AuctionId,
    string ItemId,
    string SellerId,
    decimal StartingPrice,
    DateTime StartTime,
    DateTime EndTime,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionStartedEvent(
    string AuctionId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record BidPlacedEvent(
    string AuctionId,
    string BidId,
    string BidderId,
    decimal Amount,
    decimal PreviousHighestBid,
    bool IsAutoBid,
    string? PreviousBidderId,
    int BidCount,
    DateTime BidTime,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record OutbidEvent(
    string AuctionId,
    string OutbidBidderId,
    string NewHighBidderId,
    decimal NewHighestBid,
    decimal OutbidAmount,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionExtendedEvent(
    string AuctionId,
    string TriggerByBidId,
    DateTime PreviousEndTime,
    DateTime NewEndTime,
    int ExtensionMinutes,
    int ExtensionCount,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionCancelledEvent(
    string AuctionId,
    string Reason,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionEndedEvent(
    string AuctionId,
    string? WinnerId,
    decimal FinalPrice,
    int TotalBids,
    bool ReserveMet,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionWatcherAddedEvent(
    string AuctionId,
    string UserId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionWatcherRemovedEvent(
    string AuctionId,
    string UserId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionFeatureToggledEvent(
    string AuctionId,
    bool IsFeatured,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
    
public sealed record BuyNowExecutedEvent(
    string AuctionId,
    string BuyerId,
    decimal BuyNowPrice,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionBuyNowReservedEvent(
    string AuctionId,
    string ReservationId,
    string BuyerId,
    decimal BuyNowPrice,
    decimal DepositAppliedAmount,
    decimal AmountDue,
    DateTime ExpiresAt,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionBuyNowReservationReleasedEvent(
    string AuctionId,
    string ReservationId,
    string BuyerId,
    string Reason,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
    
public sealed record AuctionAutoBidConfiguredEvent(
    string AuctionId,
    string BidderId,
    decimal MaxAmount,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
    
public sealed record AuctionSoldEvent(
    string AuctionId,
    string WinnerId,
    string SellerId,
    decimal FinalPrice,
    string Currency,
    int TotalBids,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionCompletedEvent(
    string AuctionId,
    string WinnerId,
    string SellerId,
    decimal FinalPrice,
    string Currency,
    DateTime DeliveredAt,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

/// <summary>
/// Bug #9 fix: raised when win is transferred to runner-up after defaulted winner.
/// Carries both old and new winner IDs so handlers can cancel the old winner's pending order.
/// Fires BEFORE AuctionSoldEvent so the old order is cancelled before the new one is created.
/// </summary>
public sealed record AuctionWinnerTransferredEvent(
    string AuctionId,
    string PreviousWinnerId,
    string NewWinnerId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionFailedEvent(
    string AuctionId,
    string SellerId,
    string Reason,
    decimal FinalPrice,
    string Currency,
    int TotalBids,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionSubmittedEvent(
    string AuctionId,
    string ItemId,
    string SellerId,
    bool VerifyByPlatform,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionApprovedEvent(
    string AuctionId,
    string ItemId,
    string ReviewerId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionRejectedEvent(
    string AuctionId,
    string ItemId,
    string ReviewerId,
    string Reason,
    int RejectionCount,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionScheduledEvent(
    string AuctionId,
    DateTime StartTime,
    DateTime EndTime,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionPaymentDefaultedEvent(
    string AuctionId,
    string SellerId,
    string DefaultedWinnerId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionRunnerUpOfferedEvent(
    string AuctionId,
    string SellerId,
    string BidderId,
    int RankNo,
    DateTime ExpiresAt,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionRunnerUpOfferRespondedEvent(
    string AuctionId,
    string BidderId,
    string Response,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionRelistedEvent(
    string SourceAuctionId,
    string NewAuctionId,
    string SellerId,
    string? Reason,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionTerminatedEvent(
    string AuctionId,
    string SellerId,
    string Reason,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AutoBidCascadeCappedEvent(
    string AuctionId,
    int TotalOperations,
    int RemainingEligibleAutoBids,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
