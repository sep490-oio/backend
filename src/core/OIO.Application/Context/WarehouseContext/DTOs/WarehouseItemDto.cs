namespace OIO.Application.Context.WarehouseContext.DTOs;

public sealed record WarehouseItemDto(
    Guid    Id,
    Guid    ItemId,
    Guid    InboundShipmentId,
    Guid?   StorageLocationId,
    string  ConditionOnArrival,
    string? InspectionNotes,
    string  Status,
    Guid?   InspectedBy,
    DateTime? InspectedAt,
    DateTime? ReceivedAt,
    DateTime  CreatedAt,
    DateTime? ModifiedAt,
    List<WarehouseItemMediaDto> Media);

public sealed record WarehouseItemMediaDto(
    Guid   Id,
    string ResourceType,
    bool   IsPrimary,
    int    SortOrder,
    string SecureUrl,
    string? FileName);