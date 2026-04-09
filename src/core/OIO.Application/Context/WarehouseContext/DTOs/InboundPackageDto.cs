namespace OIO.Application.Context.WarehouseContext.DTOs;

public sealed record InboundPackageDto(
    string    ClientOrderCode,
    string    ProviderCode,
    string    ShipmentMode,
    string?   ExternalCarrierName,
    string?   CarrierTrackingNumber,
    string?   SenderName,
    DateTime? ExpectedArrivalAt,
    int       ItemCount,
    string    PackageState,
    DateTime? FirstReceivedAt,
    DateTime  CreatedAt,
    decimal?  ShippingFee,
    string    DisplayStatus,
    bool      CanCancelPackage);

public sealed record InboundPackageItemDto(
    Guid     InboundShipmentId,
    Guid     ItemId,
    string?  ItemTitle,
    string?  ItemImageUrl,
    string   InboundStatus,
    Guid?    WarehouseItemId,
    string?  WarehouseItemStatus,
    Guid?    StorageLocationId,
    string?  StorageLocationLabel);

public sealed record InboundPackageDetailDto(
    string    ClientOrderCode,
    string    ProviderCode,
    string    ShipmentMode,
    string?   ExternalCarrierName,
    string?   CarrierTrackingNumber,
    string?   SenderName,
    string?   SenderPhone,
    string?   SenderAddress,
    string?   SenderWard,
    string?   SenderDistrict,
    string?   SenderProvince,
    DateTime? ExpectedArrivalAt,
    string    PackageState,
    DateTime? FirstReceivedAt,
    IReadOnlyList<string> ReceiptMedia,
    string?   ReceiptNotes,
    IReadOnlyList<InboundPackageItemDto> Items,
    DateTime  CreatedAt,
    decimal?  ShippingFee,
    string    DisplayStatus,
    string    PackageQrToken,
    bool      CanCancelPackage);
