using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;

/// <summary>
/// Append-only audit record for every state-changing decision on a
/// <see cref="WarehouseInspection"/> (inspector approve/reject, seller-driven
/// reinspection request, condition confirmation, etc.). Owned by the
/// <see cref="WarehouseInspection"/> aggregate; never mutated after Create.
/// </summary>
public sealed class WarehouseInspectionDecisionLog : BaseEntity<WarehouseInspectionDecisionLogId>, ICreatedAtEntity
{
    public WarehouseInspectionId InspectionId { get; private set; }

    /// <summary>
    /// One of: "approved", "rejected", "condition_confirmation_required",
    /// "condition_confirmed", "seller_requested_reinspection", "reviewer_re_review".
    /// String to keep the audit table forward-compatible without enum migrations.
    /// </summary>
    public string DecisionType { get; private set; } = null!;

    public string? Reason { get; private set; }

    /// <summary>Inspector or seller user id depending on <see cref="ActorRole"/>. Null for system actions.</summary>
    public UserId? ActorId { get; private set; }

    /// <summary>"inspector" | "seller" | "admin" | "system".</summary>
    public string ActorRole { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    private WarehouseInspectionDecisionLog() { }

    public static WarehouseInspectionDecisionLog Create(
        WarehouseInspectionId inspectionId,
        string decisionType,
        UserId? actorId,
        string actorRole,
        string? reason,
        DateTime nowUtc)
    {
        return new WarehouseInspectionDecisionLog
        {
            Id = WarehouseInspectionDecisionLogId.From(Guid.CreateVersion7()),
            InspectionId = inspectionId,
            DecisionType = decisionType,
            ActorId = actorId,
            ActorRole = actorRole,
            Reason = reason,
            CreatedAt = nowUtc,
        };
    }
}
