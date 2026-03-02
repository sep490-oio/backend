using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

public record AuctionCancelledEvent(string AuctionId, string Reason, DateTime OccurredAt) 
    : DomainEvent(OccurredAt);