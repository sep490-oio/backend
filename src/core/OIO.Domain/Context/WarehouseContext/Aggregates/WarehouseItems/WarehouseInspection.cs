using CSharpFunctionalExtensions;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems.Events;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using e = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;

public sealed class WarehouseInspection : AggregateRoot<WarehouseInspectionId>
{
    private readonly List<WarehouseInspectionDecisionLog> _decisionLogs = new();

    private WarehouseInspection() { }

    private WarehouseInspection(
        WarehouseInspectionId id,
        WarehouseItemId warehouseItemId,
        InboundShipmentId inboundShipmentId,
        Guid itemId,
        ItemCondition declaredCondition,
        WarehouseItemCondition conditionOnArrival,
        InspectionEvidence evidence,
        UserId inspectedBy,
        DateTime now,
        string? inspectionNotes)
        : base(id)
    {
        WarehouseItemId = warehouseItemId;
        InboundShipmentId = inboundShipmentId;
        ItemId = itemId;
        DeclaredCondition = declaredCondition;
        ConditionOnArrival = conditionOnArrival;
        InspectionNotes = inspectionNotes;
        Evidence = evidence;
        DecisionStatus = WarehouseInspectionDecisionStatus.PendingReview;
        InspectedBy = inspectedBy;
        InspectedAt = now;
        CreatedAt = now;
    }

    public WarehouseItemId WarehouseItemId { get; private set; }
    public InboundShipmentId InboundShipmentId { get; private set; }
    public Guid ItemId { get; private set; }
    public ItemCondition DeclaredCondition { get; private set; }
    public WarehouseItemCondition ConditionOnArrival { get; private set; }
    public string? InspectionNotes { get; private set; }
    public InspectionEvidence Evidence { get; private set; } = InspectionEvidence.Empty;
    public WarehouseInspectionDecisionStatus DecisionStatus { get; private set; } = WarehouseInspectionDecisionStatus.PendingReview;
    public string? DecisionReason { get; private set; }
    public UserId InspectedBy { get; private set; }
    public DateTime InspectedAt { get; private set; }
    public UserId? ReviewedBy { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public DateTime? SellerConfirmedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    /// <summary>
    /// Append-only audit trail of every decision (inspector review, seller-driven
    /// reinspection request, condition-confirmation flow). Never mutated after
    /// insertion — UI/admin can replay the full timeline.
    /// </summary>
    public IReadOnlyCollection<WarehouseInspectionDecisionLog> DecisionLogs => _decisionLogs.AsReadOnly();

    public static Result<WarehouseInspection, e> Create(
        WarehouseItemId warehouseItemId,
        InboundShipmentId inboundShipmentId,
        Guid itemId,
        ItemCondition declaredCondition,
        WarehouseItemCondition conditionOnArrival,
        InspectionEvidence evidence,
        UserId inspectedBy,
        DateTime now,
        string? inspectionNotes = null)
    {
        if (!evidence.HasAny())
            return WarehouseErrors.Inspection.EvidenceRequired;

        return new WarehouseInspection(
            WarehouseInspectionId.From(Guid.CreateVersion7()),
            warehouseItemId,
            inboundShipmentId,
            itemId,
            declaredCondition,
            conditionOnArrival,
            evidence,
            inspectedBy,
            now,
            inspectionNotes);
    }

    public UnitResult<e> Approve(UserId reviewerId, DateTime now)
    {
        if (DecisionStatus != WarehouseInspectionDecisionStatus.PendingReview)
            return WarehouseErrors.Inspection.CannotReview;

        DecisionStatus = WarehouseInspectionDecisionStatus.Approved;
        DecisionReason = null;
        ReviewedBy = reviewerId;
        ReviewedAt = now;
        ModifiedAt = now;

        _decisionLogs.Add(WarehouseInspectionDecisionLog.Create(
            inspectionId: Id,
            decisionType: "approved",
            actorId: reviewerId,
            actorRole: "inspector",
            reason: null,
            nowUtc: now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> RequireConditionConfirmation(UserId reviewerId, DateTime now)
    {
        if (DecisionStatus != WarehouseInspectionDecisionStatus.PendingReview)
            return WarehouseErrors.Inspection.CannotReview;

        DecisionStatus = WarehouseInspectionDecisionStatus.ConditionConfirmationRequired;
        DecisionReason = null;
        ReviewedBy = reviewerId;
        ReviewedAt = now;
        ModifiedAt = now;

        _decisionLogs.Add(WarehouseInspectionDecisionLog.Create(
            inspectionId: Id,
            decisionType: "condition_confirmation_required",
            actorId: reviewerId,
            actorRole: "inspector",
            reason: null,
            nowUtc: now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> Reject(UserId reviewerId, string reason, DateTime now)
    {
        if (DecisionStatus != WarehouseInspectionDecisionStatus.PendingReview)
            return WarehouseErrors.Inspection.CannotReview;

        DecisionStatus = WarehouseInspectionDecisionStatus.Rejected;
        DecisionReason = reason;
        ReviewedBy = reviewerId;
        ReviewedAt = now;
        ModifiedAt = now;

        _decisionLogs.Add(WarehouseInspectionDecisionLog.Create(
            inspectionId: Id,
            decisionType: "rejected",
            actorId: reviewerId,
            actorRole: "inspector",
            reason: reason,
            nowUtc: now));

        RaiseDomainEvent(new WarehouseInspectionRejectedEvent(
            WarehouseInspectionId: Id.Value,
            WarehouseItemId: WarehouseItemId.Value,
            RejectedBy: reviewerId.Value,
            Reason: reason,
            OccurredAt: now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> ConfirmSellerCondition(DateTime now)
    {
        if (DecisionStatus != WarehouseInspectionDecisionStatus.ConditionConfirmationRequired)
            return WarehouseErrors.Inspection.ConditionConfirmationNotRequired;

        DecisionStatus = WarehouseInspectionDecisionStatus.ConditionConfirmed;
        SellerConfirmedAt = now;
        ModifiedAt = now;

        _decisionLogs.Add(WarehouseInspectionDecisionLog.Create(
            inspectionId: Id,
            decisionType: "condition_confirmed",
            actorId: null,
            actorRole: "seller",
            reason: null,
            nowUtc: now));

        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Seller-initiated re-inspection request — only allowed once the inspection
    /// is <see cref="WarehouseInspectionDecisionStatus.Rejected"/>. Resets
    /// <see cref="DecisionStatus"/> back to PendingReview so a warehouse inspector
    /// can review the item again. Append-only — does NOT clear prior fields like
    /// <see cref="DecisionReason"/>, <see cref="ReviewedBy"/>, or <see cref="ReviewedAt"/>;
    /// a fresh decision will overwrite them via the next Approve/Reject/Require call.
    /// </summary>
    public UnitResult<e> RequestReinspectionBySeller(UserId sellerId, string? reason, DateTime now)
    {
        if (DecisionStatus != WarehouseInspectionDecisionStatus.Rejected)
            return e.Conflict(
                "WarehouseInspection.NotRejected",
                "Re-inspection can only be requested when the warehouse inspection is in 'rejected' state.");

        var reinspectionCount = _decisionLogs.Count(x => x.DecisionType == "seller_requested_reinspection");
        if (reinspectionCount >= 1)
            return e.Conflict(
                "WarehouseInspection.MaxReinspectionsReached",
                "Chỉ được phép yêu cầu kiểm định lại 1 lần duy nhất.");

        DecisionStatus = WarehouseInspectionDecisionStatus.PendingReview;
        ModifiedAt = now;

        _decisionLogs.Add(WarehouseInspectionDecisionLog.Create(
            inspectionId: Id,
            decisionType: "seller_requested_reinspection",
            actorId: sellerId,
            actorRole: "seller",
            reason: reason,
            nowUtc: now));

        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Moderator-initiated re-inspection request via Dispute Resolution.
    /// Resets <see cref="DecisionStatus"/> back to PendingReview.
    /// Can be invoked when the inspection is either PendingReview or Rejected.
    /// </summary>
    public UnitResult<e> ForceReinspectionByModerator(UserId moderatorId, string? reason, DateTime now)
    {
        if (DecisionStatus != WarehouseInspectionDecisionStatus.PendingReview && 
            DecisionStatus != WarehouseInspectionDecisionStatus.Rejected)
        {
            return e.Conflict(
                "WarehouseInspection.InvalidStateForForcedReinspection",
                "Moderator can only force re-inspection when the item is pending review or rejected.");
        }

        DecisionStatus = WarehouseInspectionDecisionStatus.PendingReview;
        ModifiedAt = now;

        _decisionLogs.Add(WarehouseInspectionDecisionLog.Create(
            inspectionId: Id,
            decisionType: "moderator_forced_reinspection",
            actorId: moderatorId,
            actorRole: "moderator",
            reason: reason,
            nowUtc: now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> RefreshEvidenceSnapshot(
        string oldPublicId,
        StorageRef storageRef,
        MediaInfo info,
        DateTime now)
    {
        Evidence = Evidence.RefreshSnapshot(oldPublicId, storageRef, info);
        ModifiedAt = now;
        return UnitResult.Success<e>();
    }

    public UnitResult<e> Update(
        WarehouseItemCondition conditionOnArrival,
        InspectionEvidence evidence,
        UserId inspectedBy,
        DateTime now,
        string? inspectionNotes = null)
    {
        if (DecisionStatus != WarehouseInspectionDecisionStatus.PendingReview)
            return e.Conflict("WarehouseInspection.InvalidState", "Can only update inspection when pending review.");

        if (!evidence.HasAny())
            return WarehouseErrors.Inspection.EvidenceRequired;

        ConditionOnArrival = conditionOnArrival;
        Evidence = evidence;
        InspectedBy = inspectedBy;
        InspectedAt = now;
        InspectionNotes = inspectionNotes;
        ModifiedAt = now;

        return UnitResult.Success<e>();
    }
}
