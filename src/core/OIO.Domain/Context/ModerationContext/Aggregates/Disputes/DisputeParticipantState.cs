using CSharpFunctionalExtensions;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.ModerationContext.Aggregates.Disputes;

public sealed class DisputeParticipantState : BaseEntity<DisputeParticipantStateId>, ICreatedAtEntity
{
    public DisputeId DisputeId { get; private set; }
    public UserId UserId { get; private set; }
    public DisputeMessageId? LastReadMessageId { get; private set; }
    public DateTime? LastReadAt { get; private set; }
    public DateTime? LastSeenAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    public Dispute Dispute { get; private set; } = null!;

    private DisputeParticipantState() { }

    public static DisputeParticipantState Create(
        DisputeId disputeId,
        UserId userId,
        DateTime nowUtc,
        DisputeMessageId? lastReadMessageId = null,
        DateTime? lastReadAt = null)
    {
        return new DisputeParticipantState
        {
            Id = DisputeParticipantStateId.From(Guid.CreateVersion7()),
            DisputeId = disputeId,
            UserId = userId,
            LastReadMessageId = lastReadMessageId,
            LastReadAt = lastReadAt,
            LastSeenAt = lastReadAt,
            CreatedAt = nowUtc,
            ModifiedAt = lastReadAt.HasValue ? nowUtc : null
        };
    }

    public UnitResult<Error> MarkRead(
        DisputeMessageId lastReadMessageId,
        DateTime readAt)
    {
        LastReadMessageId = lastReadMessageId;
        LastReadAt = readAt;
        LastSeenAt = readAt;
        ModifiedAt = readAt;
        return UnitResult.Success<Error>();
    }

    public void Touch(DateTime nowUtc)
    {
        LastSeenAt = nowUtc;
        ModifiedAt = nowUtc;
    }
}
