using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;

/// <summary>
/// A photo or document linked to a <see cref="WarehouseToSellerShipment"/>.
/// Mirrors <c>OutboundShipmentEvidence</c> — category distinguishes warehouse-staff
/// pickup photos vs seller-receipt photos.
/// </summary>
public sealed class WarehouseToSellerShipmentEvidence
    : BaseEntity<WarehouseToSellerShipmentEvidenceId>, ICreatedAtEntity
{
    private WarehouseToSellerShipmentEvidence() { }

    internal WarehouseToSellerShipmentEvidence(
        WarehouseToSellerShipmentEvidenceId id,
        WarehouseToSellerShipmentId shipmentId,
        string category,
        MediaUploadId mediaUploadId,
        string? secureUrl,
        string? fileName,
        string resourceType,
        DateTime createdAt,
        UserId createdBy)
    {
        Id             = id;
        ShipmentId     = shipmentId;
        Category       = category;
        MediaUploadId  = mediaUploadId;
        SecureUrl      = secureUrl;
        FileName       = fileName;
        ResourceType   = resourceType;
        CreatedAt      = createdAt;
        CreatedBy      = createdBy;
    }

    public WarehouseToSellerShipmentId ShipmentId { get; private set; }
    public string Category { get; private set; } = null!;
    public MediaUploadId MediaUploadId { get; private set; }
    public string? SecureUrl { get; private set; }
    public string? FileName { get; private set; }
    public string ResourceType { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public UserId CreatedBy { get; private set; }
}
