using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.CatalogContext.Aggregates.Items;

public sealed class ItemModerationReview : BaseEntity<ItemModerationReviewId>, ICreatedAtEntity
{
    public ItemId ItemId { get; private set; }
    public ModerationAction Action { get; private set; }
    public UserId ReviewerId { get; private set; }
    public string? Reason { get; private set; }
    public string? OldStatus { get; private set; }
    public string? NewStatus { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public Item Item { get; private set; } = null!;

    private ItemModerationReview() { }

    public static ItemModerationReview Create(
        ItemId itemId,
        ModerationAction action,
        UserId reviewerId,
        string? oldStatus,
        string? newStatus,
        DateTime nowUtc,
        string? reason = null)
    {
        return new ItemModerationReview
        {
            Id = ItemModerationReviewId.From(Guid.CreateVersion7()),
            ItemId = itemId,
            Action = action,
            ReviewerId = reviewerId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Reason = reason,
            CreatedAt = nowUtc
        };
    }
}
