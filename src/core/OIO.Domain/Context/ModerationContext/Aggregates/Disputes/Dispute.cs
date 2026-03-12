using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ModerationContext.Aggregates.Disputes;

public sealed class Dispute : AggregateRoot<DisputeId>, IAuditableEntity
{
    private readonly List<DisputeEvidence> _evidence = [];
    private readonly List<DisputeMessage> _messages = [];
    private readonly List<DisputeRefund> _refunds = [];
    private readonly List<DisputeStatusHistory> _statusHistory = [];

    public DisputeNumber DisputeNumber { get; private set; }
    public OrderId OrderId { get; private set; }
    public AuctionId AuctionId { get; private set; }
    public UserId ComplainantId { get; private set; }
    public UserId RespondentId { get; private set; }
    public DisputeType Type { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public DesiredResolution DesiredResolution { get; private set; }
    public DisputeStatus Status { get; private set; }
    public DisputePriority Priority { get; private set; }
    public ResolutionType ResolutionType { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public decimal? ResolutionAmount { get; private set; }
    public UserId? AssignedTo { get; private set; }
    public UserId? EscalatedTo { get; private set; }
    public DateTime? ResponseDeadline { get; private set; }
    public DateTime? EscalatedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    // Navigation
    public IReadOnlyCollection<DisputeEvidence> Evidence => _evidence.AsReadOnly();
    public IReadOnlyCollection<DisputeMessage> Messages => _messages.AsReadOnly();
    public IReadOnlyCollection<DisputeRefund> Refunds => _refunds.AsReadOnly();
    public IReadOnlyCollection<DisputeStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    private Dispute() { }
}