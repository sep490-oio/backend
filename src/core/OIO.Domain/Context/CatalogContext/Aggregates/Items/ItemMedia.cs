using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Entities;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;
using ItemMediaId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemMediaId;

namespace OIO.Domain.Context.CatalogContext.Aggregates.Items;

public sealed class ItemMedia : BaseEntity<ItemMediaId>, ICreatedAtEntity
{
    public ItemId ItemId { get; private set; }
    public string ResourceType { get; private set; }
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }
    public MediaInfo Info { get; private set; }
    public StorageRef StorageRef { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    public Item Item {get; private set;}

    private ItemMedia() { }

    public static ItemMedia Create(
        DateTime nowUtc,
        ItemId itemId,
        string resourceType,
        bool isPrimary,
        int sortOrder,
        StorageRef storageRef,
        MediaInfo info)
    {
        return new ItemMedia
        {
            Id = ItemMediaId.From(Guid.CreateVersion7()),
            ItemId = itemId,
            ResourceType = resourceType,
            IsPrimary = isPrimary,
            SortOrder = sortOrder,
            Info =  info,
            StorageRef = storageRef,
            CreatedAt = nowUtc,
        };
    }

    public void SetAsPrimary() => IsPrimary = true;
    public void UnsetPrimary() => IsPrimary = false;
    public void Reorder(int sortOrder) => SortOrder = sortOrder;

    public void RefreshMediaSnapshot(StorageRef storageRef, MediaInfo info)
    {
        StorageRef = storageRef;
        Info = info;
    }
   
}
