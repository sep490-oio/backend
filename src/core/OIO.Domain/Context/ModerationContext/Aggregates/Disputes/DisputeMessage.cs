using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ModerationContext.Aggregates.Disputes;

public sealed class DisputeMessage : BaseEntity<DisputeMessageId>, ICreatedAtEntity
{
    private readonly List<DisputeMessageAttachment> _attachments = [];

    public DisputeId DisputeId { get; private set; }
    public UserId SenderId { get; private set; }
    public string Message { get; private set; }
    public bool IsInternal { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Dispute Dispute { get; private set; } = null!;
    public IReadOnlyCollection<DisputeMessageAttachment> Attachments => _attachments.AsReadOnly();
    private DisputeMessage() { }

    public static DisputeMessage Create(
        DisputeId disputeId,
        UserId senderId,
        string message,
        DateTime nowUtc,
        bool isInternal = false)
    {
        return new DisputeMessage
        {
            Id = DisputeMessageId.From(Guid.CreateVersion7()),
            DisputeId = disputeId,
            SenderId = senderId,
            Message = message,
            IsInternal = isInternal,
            CreatedAt = nowUtc
        };
    }
}
