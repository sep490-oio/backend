namespace OIO.Application.Context.WarehouseContext.DTOs;

public sealed record WarehouseItemDto(
    Guid      Id,
    Guid      ItemId,
    Guid      InboundShipmentId,
    string?   InboundShipmentCode,
    Guid?     StorageLocationId,
    string?   StorageLocationLabel,
    string?   ItemTitle,
    Guid?     SellerId,
    string?   SellerName,
    string?   ItemImageUrl,
    string    Status,
    DateTime? ReceivedAt,
    DateTime  CreatedAt,
    DateTime? ModifiedAt,
    List<WarehouseItemMediaDto>? Media = null);

public sealed record WarehouseItemMediaDto(
    Guid   Id,
    string ResourceType,
    bool   IsPrimary,
    int    SortOrder,
    string SecureUrl,
    string? FileName);
