using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;

namespace OIO.Application.Context.WarehouseContext.Mappings;

internal static class StorageLocationMappings
{
    public static StorageLocationDto ToDto(this WarehouseStorageLocation location) =>
        new(
            Id:         location.Id.Value,
            Zone:       location.Zone,
            Aisle:      location.Aisle,
            Shelf:      location.Shelf,
            Bin:        location.Bin,
            Label:      location.Label,
            IsOccupied: location.IsOccupied,
            CreatedAt:  location.CreatedAt);
}