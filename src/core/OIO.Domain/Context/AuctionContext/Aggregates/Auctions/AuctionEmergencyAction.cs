using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionEmergencyAction : BaseEntity<AuctionEmergencyActionId>, ICreatedAtEntity
{
    public AuctionEmergencyId EmergencyId { get; private set; }
    public string ActionType { get; private set; }
    public string Payload { get; private set; }  // jsonb
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public AuctionEmergency Emergency { get; private set; } = null!;

    private AuctionEmergencyAction() { }
}