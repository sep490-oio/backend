using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;

/// <summary>
/// A photo or document linked to an outbound shipment.
/// Category distinguishes staff package photos, handover photos, and buyer receipt photos.
/// </summary>
public sealed class OutboundShipmentEvidence : BaseEntity<OutboundShipmentEvidenceId>, ICreatedAtEntity
{
    private OutboundShipmentEvidence() { }

    internal OutboundShipmentEvidence(
        OutboundShipmentEvidenceId id,
        OutboundShipmentId shipmentId,
        string category,
        MediaUploadId mediaUploadId,
        string? secureUrl,
        string? fileName,
        string resourceType,
        DateTime createdAt)
    {
        Id             = id;
        ShipmentId     = shipmentId;
        Category       = category;
        MediaUploadId  = mediaUploadId;
        SecureUrl      = secureUrl;
        FileName       = fileName;
        ResourceType   = resourceType;
        CreatedAt      = createdAt;
    }

    public OutboundShipmentId ShipmentId { get; private set; }
    public string Category { get; private set; } = null!;
    public MediaUploadId MediaUploadId { get; private set; }
    public string? SecureUrl { get; private set; }
    public string? FileName { get; private set; }
    public string ResourceType { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Refreshes the cached <see cref="SecureUrl"/> / <see cref="FileName"/>
    /// snapshot after media relocation. Aggregate-only entry-point.
    /// </summary>
    internal void UpdateSnapshot(MediaInfo info)
    {
        if (!string.IsNullOrWhiteSpace(info.SecureUrl))
            SecureUrl = info.SecureUrl;

        if (!string.IsNullOrWhiteSpace(info.FileName))
            FileName = info.FileName;
    }
}
