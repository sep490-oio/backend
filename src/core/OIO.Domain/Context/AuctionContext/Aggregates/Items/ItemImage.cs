using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Items;

public sealed class ItemImage : BaseEntity<ItemImageId>
{
    public ItemId ItemId { get; private set; }
    public string Url { get; private set; }
    public bool IsMain { get; private set; }
    public int SortOrder { get; private set; }

    internal ItemImage(ItemImageId id, ItemId itemId, string url, bool isMain, int sortOrder) : base(id)
    {
        ItemId = itemId;
        Url = url;
        IsMain = isMain;
        SortOrder = sortOrder;
    }

    internal void SetAsMain(bool isMain) => IsMain = isMain;
}