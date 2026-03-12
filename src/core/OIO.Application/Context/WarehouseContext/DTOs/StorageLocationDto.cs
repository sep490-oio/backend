namespace OIO.Application.Context.WarehouseContext.DTOs;

public sealed record StorageLocationDto(
    Guid   Id,
    string Zone,
    string Aisle,
    string Shelf,
    string Bin,
    string Label,
    bool   IsOccupied,
    DateTime CreatedAt);