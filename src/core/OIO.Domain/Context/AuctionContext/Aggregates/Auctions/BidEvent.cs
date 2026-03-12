using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class BidEvent : BaseEntity<BidEventId>, ICreatedAtEntity
{
    public BidId BidId { get; private set; }
    public string EventType { get; private set; }
    public string? ReasonCode { get; private set; }
    public string Payload { get; private set; }  // jsonb
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Bid Bid { get; private set; } = null!;

    private BidEvent() { }
}