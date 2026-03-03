using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Items;

public sealed class ItemImage : Entity<ItemImageId>
{
    public ItemId ItemId { get; private set; }
    public string ImageUrl { get; private set; }
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ItemImage() { }

    public static ItemImage Create(
        ItemId itemId,
        string imageUrl,
        bool isPrimary = false,
        int sortOrder = 0,
        DateTime? now = null)
    {
        return new ItemImage
        {
            Id = ItemImageId.From(Guid.CreateVersion7()),
            ItemId = itemId,
            ImageUrl = imageUrl,
            IsPrimary = isPrimary,
            SortOrder = sortOrder,
            CreatedAt = now ?? DateTime.UtcNow
        };
    }

    public void SetAsPrimary() => IsPrimary = true;
    public void UnsetPrimary() => IsPrimary = false;
    public void Reorder(int sortOrder) => SortOrder = sortOrder;
}