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

public sealed record AuctionActivatedEvent(
    string AuctionId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record BidPlacedEvent(
    string AuctionId,
    string BidId,
    string BidderId,
    decimal Amount,
    bool IsAutoBid,
    string? PreviousBidderId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record OutbidEvent(
    string AuctionId,
    string OutbidBidderId,
    string NewHighBidderId,
    decimal NewHighAmount,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionExtendedEvent(
    string AuctionId,
    DateTime OldEndTime,
    DateTime NewEndTime,
    int ExtensionMinutes,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionBoughtOutEvent(
    string AuctionId,
    string BuyerId,
    decimal Amount,
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

public sealed record AutoBidSetupEvent(
    string AuctionId,
    string BidderId,
    decimal MaxAmount,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AutoBidReplacedEvent(
    string AuctionId,
    string BidderId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record DepositAddedEvent(
    string AuctionId,
    string UserId,
    decimal Amount,
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
    
public sealed record AuctionAutoBidConfiguredEvent(
    string AuctionId,
    string BidderId,
    decimal MaxAmount,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);