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

public sealed record WarehouseItemDetailDto(
    // Warehouse item
    Guid      Id,
    string    Status,
    DateTime? ReceivedAt,
    DateTime  CreatedAt,
    DateTime? ModifiedAt,
    Guid?     StorageLocationId,
    string?   StorageLocationLabel,
    Guid      InboundShipmentId,
    string?   InboundShipmentCode,
    // Original item
    Guid      ItemId,
    string?   ItemTitle,
    string?   ItemImageUrl,
    string?   Condition,
    string?   Description,
    // Seller
    Guid?     SellerId,
    string?   SellerName,
    // Receiving media
    List<WarehouseItemMediaDto> Media,
    IReadOnlyList<string>? ReceiptPhotos = null,
    // Staff action affordances
    bool      CanAssignOrMoveLocation = false,
    bool      CanBookOutbound = false,
    Guid?     OutboundBookingOrderId = null,
    bool      CanViewOutboundShipment = false,
    Guid?     OutboundShipmentId = null,
    // Inspection
    WarehouseInspectionDto? Inspection = null);
