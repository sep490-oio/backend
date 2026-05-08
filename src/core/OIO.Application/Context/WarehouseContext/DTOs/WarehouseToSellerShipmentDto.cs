namespace OIO.Application.Context.WarehouseContext.DTOs;

/// <summary>
/// Read model for <c>WarehouseToSellerShipment</c> rows returned to the seller
/// or warehouse-staff UIs. <see cref="SellerAddressSnapshot"/> is raw JSON — the
/// FE parses it. <see cref="Item"/> is an optional summary for list rendering.
/// </summary>
public sealed record WarehouseToSellerShipmentDto(
    Guid     Id,
    Guid     WarehouseItemId,
    Guid     WarehouseInspectionId,
    Guid     SellerId,
    string   SellerAddressSnapshot,
    string   RejectionReason,
    string?  ProviderCode,
    string?  TrackingNumber,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    DateTime? SellerConfirmedAt,
    string?  DeliveryFailureReason,
    string   Status,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    WarehouseToSellerShipmentItemSummaryDto? Item,
    string?  SellerDisplayName = null,
    string?  QrToken = null,
    bool     HasReceiptEvidence = false,
    IReadOnlyList<WarehouseToSellerShipmentEvidenceDto>? Evidence = null);

public sealed record WarehouseToSellerShipmentItemSummaryDto(
    Guid     WarehouseItemId,
    Guid     ItemId,
    string?  ItemTitle,
    string?  PrimaryImageUrl,
    string   WarehouseItemStatus);

