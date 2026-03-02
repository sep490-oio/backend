using OIO.Domain.SeedWork.DomainEvents;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

public record BidPlacedEvent(string AuctionId, Guid BidderId, Money Amount, DateTime OccurredAt) 
    : DomainEvent(OccurredAt);