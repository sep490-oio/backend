using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Aggregates.Disputes;

public sealed class Dispute : AggregateRoot<DisputeId>, IAuditableEntity
{
    private readonly List<DisputeEvidence> _evidence = [];
    private readonly List<DisputeMessage> _messages = [];
    private readonly List<DisputeRefund> _refunds = [];
    private readonly List<DisputeStatusHistory> _statusHistory = [];

    public DisputeNumber DisputeNumber { get; private set; }
    public OrderId OrderId { get; private set; }
    public AuctionId? AuctionId { get; private set; }
    public IdentityVerificationId? VerificationId { get; private set; }
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

    public IReadOnlyCollection<DisputeEvidence> Evidence => _evidence.AsReadOnly();
    public IReadOnlyCollection<DisputeMessage> Messages => _messages.AsReadOnly();
    public IReadOnlyCollection<DisputeRefund> Refunds => _refunds.AsReadOnly();
    public IReadOnlyCollection<DisputeStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    private Dispute() { }

    public static Result<Dispute, Error> Create(
        AuctionId auctionId,
        UserId complainantId,
        UserId respondentId,
        DisputeType type,
        string title,
        string description,
        DateTime nowUtc,
        DesiredResolution? desiredResolution = null,
        DisputePriority? priority = null,
        OrderId? orderId = null)
        => CreateCore(
            auctionId,
            null,
            complainantId,
            respondentId,
            type,
            title,
            description,
            nowUtc,
            desiredResolution,
            priority,
            orderId);

    public static Result<Dispute, Error> CreateForVerification(
        IdentityVerificationId verificationId,
        UserId complainantId,
        UserId respondentId,
        DisputeType type,
        string title,
        string description,
        DateTime nowUtc,
        DesiredResolution? desiredResolution = null,
        DisputePriority? priority = null)
        => CreateCore(
            null,
            verificationId,
            complainantId,
            respondentId,
            type,
            title,
            description,
            nowUtc,
            desiredResolution,
            priority,
            null);

    private static Result<Dispute, Error> CreateCore(
        AuctionId? auctionId,
        IdentityVerificationId? verificationId,
        UserId complainantId,
        UserId respondentId,
        DisputeType type,
        string title,
        string description,
        DateTime nowUtc,
        DesiredResolution? desiredResolution = null,
        DisputePriority? priority = null,
        OrderId? orderId = null)
    {
        if (auctionId is null && verificationId is null)
        {
            return Error.Validation(
                "reference",
                "Dispute.ReferenceRequired",
                "A dispute must be linked to either an auction or a verification.");
        }

        var (_, isFailure, disputeNumber, error) = DisputeNumber.Create($"DSP-{Guid.NewGuid():N}"[..16]);
        if (isFailure) return error;

        var dispute = new Dispute
        {
            Id = DisputeId.From(Guid.CreateVersion7()),
            DisputeNumber = disputeNumber,
            OrderId = orderId ?? OrderId.From(Guid.Empty),
            AuctionId = auctionId,
            VerificationId = verificationId,
            ComplainantId = complainantId,
            RespondentId = respondentId,
            Type = type,
            Title = title,
            Description = description,
            DesiredResolution = desiredResolution ?? Enums.DesiredResolution.NoAction,
            Status = DisputeStatus.Open,
            Priority = priority ?? DisputePriority.Medium,
            ResolutionType = Enums.ResolutionType.NoResolution,
            CreatedAt = nowUtc
        };

        dispute._statusHistory.Add(DisputeStatusHistory.Create(
            disputeId: dispute.Id,
            oldStatus: null,
            newStatus: DisputeStatus.Open.Id,
            changedBy: complainantId,
            reason: "Dispute created",
            nowUtc: nowUtc));

        return dispute;
    }

    public DisputeMessage AddMessage(UserId senderId, string message, DateTime nowUtc, bool isInternal = false)
    {
        var msg = DisputeMessage.Create(Id, senderId, message, nowUtc, isInternal);
        _messages.Add(msg);
        ModifiedAt = nowUtc;
        return msg;
    }

    public UnitResult<Error> Resolve(
        ResolutionType resolutionType,
        UserId resolvedBy,
        DateTime nowUtc,
        string? notes = null,
        decimal? amount = null)
    {
        if (Status == DisputeStatus.Resolved || Status == DisputeStatus.Closed)
            return Error.Conflict("Dispute.AlreadyResolved", "Dispute has already been resolved.");

        var oldStatus = Status.Id;
        Status = DisputeStatus.Resolved;
        ResolutionType = resolutionType;
        ResolutionNotes = notes;
        ResolutionAmount = amount;
        ResolvedAt = nowUtc;
        ModifiedAt = nowUtc;

        _statusHistory.Add(DisputeStatusHistory.Create(
            disputeId: Id,
            oldStatus: oldStatus,
            newStatus: DisputeStatus.Resolved.Id,
            changedBy: resolvedBy,
            reason: $"Resolved: {resolutionType.Id}",
            nowUtc: nowUtc));

        return UnitResult.Success<Error>();
    }

    public void AssignTo(UserId adminId, DateTime nowUtc)
    {
        AssignedTo = adminId;
        Status = DisputeStatus.UnderReview;
        ModifiedAt = nowUtc;

        _statusHistory.Add(DisputeStatusHistory.Create(
            disputeId: Id,
            oldStatus: Status.Id,
            newStatus: DisputeStatus.UnderReview.Id,
            changedBy: adminId,
            reason: "Assigned to admin",
            nowUtc: nowUtc));
    }
}
