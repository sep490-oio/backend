using OIO.Domain.SeedWork.DomainEvents;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Bids.Events;

public record BidPlacedDomainEvent(
    string BidId, 
    string AuctionId, 
    Guid BidderId, 
    Money Amount, 
    DateTime OccurredAt) : DomainEvent(OccurredAt);