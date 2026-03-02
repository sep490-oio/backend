using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Bids.Events;

public record BidOutbidDomainEvent(
    string BidId, 
    string AuctionId, 
    Guid BidderId, 
    DateTime OccurredAt) : DomainEvent(OccurredAt);