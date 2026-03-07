using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Items;

public sealed class ItemMedia : BaseEntity<ItemMediaId>, ICreatedAtEntity
{
    public ItemId ItemId { get; private set; }
    public string PublicId { get; private set; }
    public string Url { get; private set; }
    public string ResourceType { get; private set; }
    public bool IsPrimary { get; private set; }
    public int SortOrder { get; private set; }
    
    public string? FileName { get; private set; }
    public long? Bytes { get; private set; }
    public string? Format { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public double? DurationSeconds { get; private set; }
    
    public DateTime CreatedAt { get; private set; }

    private ItemMedia() { }

    public static ItemMedia Create(
        DateTime nowUtc,
        ItemId itemId,
        string url,
        string publicId,
        string resourceType,
        bool isPrimary,
        int sortOrder,
        string? fileName = null,
        long? bytes = null,
        string? format = null,
        int? width = null,
        int? height = null,
        double? durationSeconds = null)
    {
        return new ItemMedia
        {
            Id = ItemMediaId.From(Guid.CreateVersion7()),
            ItemId = itemId,
            Url = url,
            PublicId = publicId,
            ResourceType = resourceType,
            IsPrimary = isPrimary,
            SortOrder = sortOrder,
            FileName = fileName,
            Bytes = bytes,
            Format = format,
            Width = width,
            Height = height,
            DurationSeconds = durationSeconds,
            CreatedAt = nowUtc,
        };
    }

    public void SetAsPrimary() => IsPrimary = true;
    public void UnsetPrimary() => IsPrimary = false;
    public void Reorder(int sortOrder) => SortOrder = sortOrder;
    
    public bool IsImage => ResourceType == "image";
    public bool IsVideo => ResourceType == "video";
    public bool IsDocument => ResourceType == "raw";
}