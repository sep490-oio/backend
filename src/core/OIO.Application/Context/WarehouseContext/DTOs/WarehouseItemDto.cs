namespace OIO.Application.Context.WarehouseContext.DTOs;

public sealed record WarehouseItemDto(
    Guid    Id,
    Guid    ItemId,
    Guid    InboundShipmentId,
    Guid?   StorageLocationId,
    string  Status,
    DateTime? ReceivedAt,
    DateTime  CreatedAt,
    DateTime? ModifiedAt);
