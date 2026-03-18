using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ModerationContext.Aggregates.Disputes;

public sealed class DisputeEvidence : BaseEntity<DisputeEvidenceId>, ICreatedAtEntity
{
    public DisputeId DisputeId { get; private set; }
    public UserId SubmittedBy { get; private set; }
    public EvidenceType Type { get; private set; }
    public StorageRef? EvidenceStorage { get; private set; }
    public MediaInfo? EvidenceInfo { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Dispute Dispute { get; private set; } = null!;
    private DisputeEvidence() { }
}