using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ModerationContext.Aggregates.Disputes;

public sealed class DisputeMessage : BaseEntity<DisputeMessageId>, ICreatedAtEntity
{
    public DisputeId DisputeId { get; private set; }
    public UserId SenderId { get; private set; }
    public string Message { get; private set; }
    public bool IsInternal { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Dispute Dispute { get; private set; } = null!;
    private DisputeMessage() { }
}