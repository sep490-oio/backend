using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;

namespace OIO.Application.Context.WarehouseContext.Mappings;

internal static class WarehouseItemMappings
{
    public static WarehouseItemDto ToDto(this WarehouseItem item) =>
        new(
            Id:                item.Id.Value,
            ItemId:            item.ItemId,
            InboundShipmentId: item.InboundShipmentId.Value,
            StorageLocationId: item.StorageLocationId?.Value,
            ConditionOnArrival: item.ConditionOnArrival.Id,
            InspectionNotes:   item.InspectionNotes,
            Status:            item.Status.Id,
            InspectedBy:       item.InspectedBy?.Value,
            InspectedAt:       item.InspectedAt,
            ReceivedAt:        item.ReceivedAt,
            CreatedAt:         item.CreatedAt,
            ModifiedAt:        item.ModifiedAt);
}