using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;

public sealed class WarehouseItemMedia : BaseEntity<WarehouseItemMediaId>, ICreatedAtEntity
{
    public WarehouseItemId WarehouseItemId { get; private set; }
    public string ResourceType { get; private set; }
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }
    public MediaInfo Info { get; private set; }
    public StorageRef StorageRef { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    
    public WarehouseItem WarehouseItem { get; private set; }

    private WarehouseItemMedia() { }

    public static WarehouseItemMedia Create(
        DateTime nowUtc,
        WarehouseItemId warehouseItemId,
        string resourceType,
        bool isPrimary,
        int sortOrder,
        StorageRef storageRef,
        MediaInfo info)
    {
        return new WarehouseItemMedia
        {
            Id = WarehouseItemMediaId.From(Guid.CreateVersion7()),
            WarehouseItemId = warehouseItemId,
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
}
