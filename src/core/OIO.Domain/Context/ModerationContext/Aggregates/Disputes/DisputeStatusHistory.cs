using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ModerationContext.Aggregates.Disputes;

public sealed class DisputeStatusHistory : BaseEntity<DisputeStatusHistoryId>, ICreatedAtEntity
{
    public DisputeId DisputeId { get; private set; }
    public string? OldStatus { get; private set; }
    public string NewStatus { get; private set; }
    public UserId? ChangedBy { get; private set; }
    public string? Reason { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Dispute Dispute { get; private set; } = null!;
    private DisputeStatusHistory() { }

    public static DisputeStatusHistory Create(
        DisputeId disputeId,
        string? oldStatus,
        string newStatus,
        UserId? changedBy,
        string? reason,
        DateTime nowUtc)
    {
        return new DisputeStatusHistory
        {
            Id = DisputeStatusHistoryId.From(Guid.CreateVersion7()),
            DisputeId = disputeId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedBy = changedBy,
            Reason = reason,
            CreatedAt = nowUtc
        };
    }
}
