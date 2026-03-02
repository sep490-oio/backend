using OIO.Domain.SeedWork.DomainEvents;
using OIO.Domain.SeedWork.Shared;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

public record AuctionEndedEarlyEvent(string AuctionId, Guid WinnerId, Money FinalPrice, DateTime OccurredAt) 
    : DomainEvent(OccurredAt);