using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;

namespace OIO.Application.Context.AuctionContext.Mappings;

public static class ItemImageMappings
{
    public static ItemMediaDto ToDto(this ItemMedia media)
    {
        return new ItemMediaDto(
            Id: media.Id.Value,
            Url: media.Url,
            PublicId: media.PublicId,
            ResourceType: media.ResourceType,
            IsPrimary: media.IsPrimary,
            SortOrder: media.SortOrder,
            FileName: media.FileName,
            Bytes: media.Bytes,
            Format: media.Format,
            Width: media.Width,
            Height: media.Height,
            DurationSeconds: media.DurationSeconds);
    }
}