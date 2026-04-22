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
}
