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
    private readonly List<DisputeFinding> _findings = [];

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

    // ── Case-engine fields (Phase 1) ──
    public string? CaseDomain { get; private set; }
    public string? CaseType { get; private set; }
    public string? PrimaryTargetType { get; private set; }
    public Guid? CaseOrderId { get; private set; }
    public Guid? CaseAuctionId { get; private set; }
    public Guid? ShipmentId { get; private set; }
    public Guid? WarehouseItemId { get; private set; }
    public Guid? PaymentId { get; private set; }
    public string? ResolutionOutcome { get; private set; }
    public string? ResolutionReason { get; private set; }
    public string? ResolutionActionSetJson { get; private set; }
    public Guid? ResolvedBy { get; private set; }
    public DateTime? CaseResolvedAt { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public string? ContextSnapshotJson { get; private set; }

    public IReadOnlyCollection<DisputeEvidence> Evidence => _evidence.AsReadOnly();
    public IReadOnlyCollection<DisputeMessage> Messages => _messages.AsReadOnly();
    public IReadOnlyCollection<DisputeRefund> Refunds => _refunds.AsReadOnly();
    public IReadOnlyCollection<DisputeStatusHistory> StatusHistory => _statusHistory.AsReadOnly();
    public IReadOnlyCollection<DisputeFinding> Findings => _findings.AsReadOnly();

    private Dispute() { }

    [Obsolete("Internal/system use only. For user-driven dispute intake, use IDisputeIntakeService.CreateDisputeAsync which routes through CreateCase to enforce DisputeEligibilityRule invariant.", error: false)]
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

    [Obsolete("Internal/system use only. For user-driven dispute intake, use IDisputeIntakeService.CreateDisputeAsync which routes through CreateCase to enforce DisputeEligibilityRule invariant.", error: false)]
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

    public static Result<Dispute, Error> CreateCase(
        UserId complainantId,
        UserId respondentId,
        DisputeType type,
        string title,
        string description,
        DateTime nowUtc,
        string roleKey,
        string domain,
        string caseType,
        string primaryTargetType,
        DesiredResolution? desiredResolution = null,
        DisputePriority? priority = null,
        OrderId? orderId = null,
        AuctionId? auctionId = null,
        IdentityVerificationId? verificationId = null,
        Guid? shipmentId = null,
        Guid? warehouseItemId = null,
        Guid? paymentId = null,
        string? contextSnapshotJson = null)
    {
        // ── Eligibility invariant (defense-in-depth) ──
        // Closes IDisputeIntakeService bypass + EscalateReportToDispute admin path.
        // Pure enum membership check, no DB.
        if (!DisputeEligibilityRule.IsAllowed(roleKey, primaryTargetType, domain, caseType))
        {
            var allowedDomains = DisputeEligibilityRule.AllowedDomainsFor(roleKey, primaryTargetType);
            if (allowedDomains.Count == 0)
                return Error.Validation("role", "Dispute.RoleNotAllowed",
                    $"Role '{roleKey}' may not file disputes against '{primaryTargetType}'.");
            if (!allowedDomains.Contains(domain, StringComparer.Ordinal))
                return Error.Validation("domain", "Dispute.DomainMismatch",
                    $"Domain '{domain}' not allowed for role '{roleKey}' on '{primaryTargetType}'.");
            return Error.Validation("caseType", "Dispute.CaseTypeMismatch",
                $"CaseType '{caseType}' not allowed under domain '{domain}' for role '{roleKey}'.");
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
            CreatedAt = nowUtc,
            CaseDomain = domain,
            CaseType = caseType,
            PrimaryTargetType = primaryTargetType,
            CaseOrderId = orderId?.Value,
            CaseAuctionId = auctionId?.Value,
            ShipmentId = shipmentId,
            WarehouseItemId = warehouseItemId,
            PaymentId = paymentId,
            ContextSnapshotJson = contextSnapshotJson
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

    // ── Case-engine domain methods (Phase 1) ──

    public UnitResult<Error> Assign(Guid userId, DateTime nowUtc)
    {
        AssignedToUserId = userId;
        AssignedAt = nowUtc;
        ModifiedAt = nowUtc;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> TransitionTo(DisputeStatus newStatus, DateTime nowUtc)
    {
        if (!Status.CanTransitionTo(newStatus))
        {
            return Error.Conflict(
                "Dispute.InvalidTransition",
                $"Cannot transition from '{Status.Id}' to '{newStatus.Id}'.");
        }

        var oldStatus = Status.Id;
        Status = newStatus;
        ModifiedAt = nowUtc;

        _statusHistory.Add(DisputeStatusHistory.Create(
            disputeId: Id,
            oldStatus: oldStatus,
            newStatus: newStatus.Id,
            changedBy: null,
            reason: $"Transitioned to {newStatus.Id}",
            nowUtc: nowUtc));

        return UnitResult.Success<Error>();
    }

    public void AddFinding(DisputeFinding finding)
    {
        _findings.Add(finding);
    }

    public UnitResult<Error> ResolveCase(
        string outcome,
        string reason,
        string? actionSetJson,
        Guid resolvedByUserId,
        DateTime nowUtc)
    {
        if (Status.IsTerminal)
            return Error.Conflict("Dispute.AlreadyTerminal", "Dispute is already in a terminal state.");

        if (!Status.CanTransitionTo(DisputeStatus.Resolved))
            return Error.Conflict("Dispute.InvalidTransition",
                $"Cannot resolve dispute from status '{Status.Id}'.");

        var oldStatus = Status.Id;
        Status = DisputeStatus.Resolved;
        ResolutionOutcome = outcome;
        ResolutionReason = reason;
        ResolutionActionSetJson = actionSetJson;
        ResolvedBy = resolvedByUserId;
        CaseResolvedAt = nowUtc;
        ResolvedAt = nowUtc;
        ModifiedAt = nowUtc;

        _statusHistory.Add(DisputeStatusHistory.Create(
            disputeId: Id,
            oldStatus: oldStatus,
            newStatus: DisputeStatus.Resolved.Id,
            changedBy: null,
            reason: $"Resolved: {outcome}",
            nowUtc: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Reject(string reason, Guid rejectedByUserId, DateTime nowUtc)
    {
        if (Status.IsTerminal)
            return Error.Conflict("Dispute.AlreadyTerminal", "Dispute is already in a terminal state.");

        if (!Status.CanTransitionTo(DisputeStatus.Rejected))
            return Error.Conflict("Dispute.InvalidTransition",
                $"Cannot reject dispute from status '{Status.Id}'.");

        var oldStatus = Status.Id;
        Status = DisputeStatus.Rejected;
        ResolutionReason = reason;
        ResolvedBy = rejectedByUserId;
        CaseResolvedAt = nowUtc;
        ModifiedAt = nowUtc;

        _statusHistory.Add(DisputeStatusHistory.Create(
            disputeId: Id,
            oldStatus: oldStatus,
            newStatus: DisputeStatus.Rejected.Id,
            changedBy: null,
            reason: $"Rejected: {reason}",
            nowUtc: nowUtc));

        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Cancel(DateTime nowUtc)
    {
        if (Status.IsTerminal)
            return Error.Conflict("Dispute.AlreadyTerminal", "Dispute is already in a terminal state.");

        if (!Status.CanTransitionTo(DisputeStatus.Cancelled))
            return Error.Conflict("Dispute.InvalidTransition",
                $"Cannot cancel dispute from status '{Status.Id}'.");

        var oldStatus = Status.Id;
        Status = DisputeStatus.Cancelled;
        ModifiedAt = nowUtc;

        _statusHistory.Add(DisputeStatusHistory.Create(
            disputeId: Id,
            oldStatus: oldStatus,
            newStatus: DisputeStatus.Cancelled.Id,
            changedBy: null,
            reason: "Cancelled",
            nowUtc: nowUtc));

        return UnitResult.Success<Error>();
    }

    /// <summary>Whether this dispute is in a non-terminal (active) state.</summary>
    public bool IsActive => !Status.IsTerminal;
}
