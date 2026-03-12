using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionEmergency : BaseEntity<AuctionEmergencyId>
{
    private readonly List<AuctionEmergencyAction> _actions = [];

    public AuctionId AuctionId { get; private set; }
    public UserId? TriggeredById { get; private set; }
    public string TriggerSource { get; private set; }
    public string Reason { get; private set; }
    public EmergencyStatus Status { get; private set; }
    public DateTime TriggeredAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    // Navigation
    public Auction Auction { get; private set; } = null!;
    public User? TriggerBy { get; private set; }
    public IReadOnlyCollection<AuctionEmergencyAction> Actions => _actions.AsReadOnly();

    private AuctionEmergency() { }
}