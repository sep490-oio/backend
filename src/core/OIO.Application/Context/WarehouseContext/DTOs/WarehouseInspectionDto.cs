namespace OIO.Application.Context.WarehouseContext.DTOs;

public sealed record WarehouseInspectionEvidenceDto(
    string PublicId,
    string Folder,
    string? SecureUrl,
    string? FileName,
    long? Bytes,
    string? Format,
    int? Width,
    int? Height,
    double? DurationSeconds);

public sealed record WarehouseInspectionDto(
    Guid Id,
    Guid WarehouseItemId,
    Guid InboundShipmentId,
    Guid ItemId,
    string DeclaredCondition,
    string ConditionOnArrival,
    string? InspectionNotes,
    string DecisionStatus,
    string? DecisionReason,
    Guid InspectedBy,
    DateTime InspectedAt,
    Guid? ReviewedBy,
    DateTime? ReviewedAt,
    DateTime? SellerConfirmedAt,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    IReadOnlyList<WarehouseInspectionEvidenceDto> Evidence);

public sealed record InspectionQueueItemDto(
    Guid InboundShipmentId,
    Guid ItemId,
    string ItemTitle,
    Guid SellerId,
    Guid? WarehouseItemId,
    Guid? InspectionId,
    string ShipmentStatus,
    string QueueStatus,
    string? CarrierTrackingNumber,
    DateTime? ArrivedAt,
    string DeclaredCondition,
    string? ConditionOnArrival,
    DateTime? InspectedAt,
    string? StorageLocationLabel = null,
    string? ItemImageUrl = null);
