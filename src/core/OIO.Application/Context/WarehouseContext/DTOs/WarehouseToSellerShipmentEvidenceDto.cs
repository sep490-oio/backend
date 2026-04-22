namespace OIO.Application.Context.WarehouseContext.DTOs;

/// <summary>
/// API projection of <c>WarehouseToSellerShipmentEvidence</c>. Returned from
/// <c>AddWarehouseReturnEvidenceCommand</c>.
/// </summary>
public sealed record WarehouseToSellerShipmentEvidenceDto(
    Guid Id,
    Guid ShipmentId,
    string Category,
    Guid MediaUploadId,
    string? SecureUrl,
    string? FileName,
    string ResourceType,
    DateTime CreatedAt,
    Guid CreatedBy);
