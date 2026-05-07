namespace OIO.Application.Context.WarehouseContext.DTOs;

/// <summary>
/// Admin recovery row for a rejected inspection that has not produced an active
/// warehouse-to-seller return shipment yet.
/// </summary>
public sealed record PendingInspectionRejectReturnDto(
    Guid InspectionId,
    Guid WarehouseItemId,
    Guid InboundShipmentId,
    Guid ItemId,
    Guid? SellerId,
    string? ItemTitle,
    string? PrimaryImageUrl,
    string? RejectionReason,
    DateTime? ReviewedAt,
    DateTime CreatedAt,
    string DecisionStatus,
    string? WarehouseItemStatus,
    bool SellerHasDefaultAddress);
