using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments;

/// <summary>
/// Child entity of the <see cref="SellerDirectShipment"/> aggregate. Captures a
/// single piece of photographic/documentary evidence attached to a shipment —
/// either by the seller (package photo, handover proof) or the buyer
/// (delivery photo). The <see cref="MediaUrl"/> is a snapshot taken at the
/// time the evidence was recorded so the aggregate does not need to re-read
/// the media upload store to render it.
/// </summary>
public sealed class SellerDirectShipmentEvidence : BaseEntity<SellerDirectShipmentEvidenceId>
{
    private SellerDirectShipmentEvidence() { }

    private SellerDirectShipmentEvidence(
        SellerDirectShipmentEvidenceId id,
        SellerDirectShipmentId shipmentId,
        SellerDirectShipmentEvidenceKind kind,
        MediaUploadId mediaUploadId,
        string mediaUrl,
        Guid createdByUserId,
        DateTime createdAt)
    {
        Id              = id;
        ShipmentId      = shipmentId;
        Kind            = kind;
        MediaUploadId   = mediaUploadId;
        MediaUrl        = mediaUrl;
        CreatedByUserId = createdByUserId;
        CreatedAt       = createdAt;
    }

    public SellerDirectShipmentId ShipmentId { get; private set; }

    public SellerDirectShipmentEvidenceKind Kind { get; private set; }

    public MediaUploadId MediaUploadId { get; private set; }

    /// <summary>Snapshot URL captured at time of attachment.</summary>
    public string MediaUrl { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    internal static SellerDirectShipmentEvidence Create(
        SellerDirectShipmentId shipmentId,
        SellerDirectShipmentEvidenceKind kind,
        MediaUploadId mediaUploadId,
        string mediaUrl,
        Guid createdByUserId,
        DateTime nowUtc)
    {
        return new SellerDirectShipmentEvidence(
            SellerDirectShipmentEvidenceId.From(Guid.CreateVersion7()),
            shipmentId,
            kind,
            mediaUploadId,
            mediaUrl,
            createdByUserId,
            nowUtc);
    }
}
