using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

public record AuctionCreatedEvent(string AuctionId, string Title, DateTime OccurredAt) 
    : DomainEvent(OccurredAt);