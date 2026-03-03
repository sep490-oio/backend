using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

public sealed record AuctionCreatedEvent(
    string AuctionId,
    string ItemId,
    string SellerId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionActivatedEvent(
    string AuctionId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record BidPlacedEvent(
    string AuctionId,
    string BidderId,
    decimal Amount,
    string BidId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record AuctionAutoExtendedEvent(
    string AuctionId,
    DateTime NewEndTime,
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
    string Status,
    string? WinnerId,
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