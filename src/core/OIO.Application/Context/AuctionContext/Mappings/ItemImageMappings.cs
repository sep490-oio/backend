using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;

namespace OIO.Application.Context.AuctionContext.Mappings;

public static class ItemImageMappings
{
    public static ItemMediaDto ToDto(this ItemMedia media)
    {
        return new ItemMediaDto(
            Id: media.Id.Value,
            Url: media.Info.SecureUrl,
            PublicId: media.StorageRef.PublicId,
            ResourceType: media.ResourceType,
            IsPrimary: media.IsPrimary,
            SortOrder: media.SortOrder,
            FileName: media.Info.FileName,
            Bytes: media.Info.Bytes,
            Format: media.Info.Format,
            Width: media.Info.Width,
            Height: media.Info.Height,
            DurationSeconds: media.Info.DurationSeconds);
    }
}