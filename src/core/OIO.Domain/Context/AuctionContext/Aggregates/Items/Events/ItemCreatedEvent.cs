using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Items.Events;

public sealed record ItemCreatedEvent(
    string ItemId,
    string SellerId,
    DateTime OccurredAt) : DomainEvent(OccurredAt);