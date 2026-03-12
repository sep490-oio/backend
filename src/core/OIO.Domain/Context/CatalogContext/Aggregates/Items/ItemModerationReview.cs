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

    // Navigation
    public Item Item { get; private set; } = null!;

    private ItemModerationReview() { }
}