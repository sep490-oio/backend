using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.ModerationContext.Aggregates.Disputes;

public sealed class DisputeMessageAttachment : BaseEntity<DisputeMessageAttachmentId>, ICreatedAtEntity
{
    public DisputeId DisputeId { get; private set; }
    public DisputeMessageId DisputeMessageId { get; private set; }
    public MediaUploadId MediaUploadId { get; private set; }
    public int SortOrder { get; private set; }
    public StorageRef StorageRef { get; private set; } = null!;
    public MediaInfo Info { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    public Dispute Dispute { get; private set; } = null!;
    public DisputeMessage DisputeMessage { get; private set; } = null!;

    private DisputeMessageAttachment() { }

    public static DisputeMessageAttachment Create(
        DisputeId disputeId,
        DisputeMessageId disputeMessageId,
        MediaUploadId mediaUploadId,
        StorageRef storageRef,
        MediaInfo info,
        int sortOrder,
        DateTime nowUtc)
    {
        return new DisputeMessageAttachment
        {
            Id = DisputeMessageAttachmentId.From(Guid.CreateVersion7()),
            DisputeId = disputeId,
            DisputeMessageId = disputeMessageId,
            MediaUploadId = mediaUploadId,
            SortOrder = sortOrder,
            StorageRef = storageRef,
            Info = info,
            CreatedAt = nowUtc
        };
    }

    public void RefreshMediaSnapshot(
        StorageRef storageRef,
        MediaInfo info,
        DateTime nowUtc)
    {
        StorageRef = storageRef;
        Info = info;
        ModifiedAt = nowUtc;
    }
}
