using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;

namespace OIO.Application.Context.WarehouseContext.Mappings;

internal static class WarehouseItemMappings
{
    public static WarehouseItemDto ToDto(this WarehouseItem item) =>
        new(
            Id:                   item.Id.Value,
            ItemId:               item.ItemId,
            InboundShipmentId:    item.InboundShipmentId.Value,
            InboundShipmentCode:  null,
            StorageLocationId:    item.StorageLocationId?.Value,
            StorageLocationLabel: null,
            ItemTitle:            null,
            SellerId:             null,
            SellerName:           null,
            ItemImageUrl:         null,
            Status:               item.Status.Id,
            ReceivedAt:           item.ReceivedAt,
            CreatedAt:            item.CreatedAt,
            ModifiedAt:           item.ModifiedAt,
            Media:                item.Media.Select(m => new WarehouseItemMediaDto(
                Id:           m.Id.Value,
                ResourceType: m.ResourceType,
                IsPrimary:    m.IsPrimary,
                SortOrder:    m.SortOrder,
                SecureUrl:    m.Info.SecureUrl,
                FileName:     m.Info.FileName
            )).OrderBy(m => m.SortOrder).ToList());
}
