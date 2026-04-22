using CSharpFunctionalExtensions;
using OIO.Domain.Context.Shared.Entities;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments.Events;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using e = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;

/// <summary>
/// Represents a shipment routing a rejected warehouse item back to its seller.
/// Created when a warehouse inspector rejects a newly-arrived WarehouseItem.
///
/// Seller address is snapshotted at creation (jsonb) to survive later address
/// changes on the seller's UserAddress entity.
///
/// Lifecycle:
///   Pending → InTransit → Delivered → Closed (seller-confirmed receipt)
///                      ↘ ReturnedToWarehouse (delivery failure)
/// </summary>
public sealed class WarehouseToSellerShipment : AggregateRoot<WarehouseToSellerShipmentId>, ICreatedAtEntity
{
    private readonly List<WarehouseToSellerShipmentEvidence> _evidence = new();

    private WarehouseToSellerShipment() { }

    private WarehouseToSellerShipment(
        WarehouseToSellerShipmentId id,
        WarehouseItemId warehouseItemId,
        WarehouseInspectionId warehouseInspectionId,
        UserId sellerId,
        string sellerAddressSnapshot,
        string rejectionReason,
        DateTime nowUtc)
    {
        Id                    = id;
        WarehouseItemId       = warehouseItemId;
        WarehouseInspectionId = warehouseInspectionId;
        SellerId              = sellerId;
        SellerAddressSnapshot = sellerAddressSnapshot;
        RejectionReason       = rejectionReason;
        Status                = WarehouseToSellerShipmentStatus.Pending;
        CreatedAt             = nowUtc;
    }

    public WarehouseItemId WarehouseItemId { get; private set; }
    public WarehouseInspectionId WarehouseInspectionId { get; private set; }
    public UserId SellerId { get; private set; }

    /// <summary>
    /// JSONB snapshot of the seller's default UserAddress at creation time.
    /// Captured so later address changes do not affect in-flight shipments.
    /// </summary>
    public string SellerAddressSnapshot { get; private set; }

    public string RejectionReason { get; private set; }
    public string? ProviderCode { get; private set; }
    public string? TrackingNumber { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? SellerConfirmedAt { get; private set; }
    public string? DeliveryFailureReason { get; private set; }
    public WarehouseToSellerShipmentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    /// <summary>
    /// Signed return-scoped QR token issued at <see cref="MarkShipped"/> time so the
    /// seller can scan the parcel on arrival. Distinct from outbound QR.
    /// Null until the shipment ships. Silent mutation — no event.
    /// </summary>
    public string? QrToken { get; private set; }

    public IReadOnlyCollection<WarehouseToSellerShipmentEvidence> Evidence => _evidence.AsReadOnly();

    /// <summary>True once at least one <see cref="WarehouseReturnEvidenceCategory.PickupByWarehouseStaff"/> photo exists.</summary>
    public bool HasPickupEvidence =>
        _evidence.Any(ev => ev.Category == WarehouseReturnEvidenceCategory.PickupByWarehouseStaff.Id);

    /// <summary>True once at least one <see cref="WarehouseReturnEvidenceCategory.ReceiptBySeller"/> photo exists.</summary>
    public bool HasReceiptEvidence =>
        _evidence.Any(ev => ev.Category == WarehouseReturnEvidenceCategory.ReceiptBySeller.Id);

    public static Result<WarehouseToSellerShipment, e> Create(
        WarehouseItemId warehouseItemId,
        WarehouseInspectionId warehouseInspectionId,
        UserId sellerId,
        string sellerAddressSnapshot,
        string rejectionReason,
        DateTime nowUtc)
    {
        if (warehouseItemId.Value == Guid.Empty)
            return WarehouseErrors.WarehouseToSellerShipment.WarehouseItemIdRequired;

        if (warehouseInspectionId.Value == Guid.Empty)
            return WarehouseErrors.WarehouseToSellerShipment.WarehouseInspectionIdRequired;

        if (sellerId.Value == Guid.Empty)
            return WarehouseErrors.WarehouseToSellerShipment.SellerIdRequired;

        if (string.IsNullOrWhiteSpace(sellerAddressSnapshot))
            return WarehouseErrors.WarehouseToSellerShipment.SellerAddressRequired;

        if (string.IsNullOrWhiteSpace(rejectionReason))
            return WarehouseErrors.WarehouseToSellerShipment.RejectionReasonRequired;

        return new WarehouseToSellerShipment(
            WarehouseToSellerShipmentId.From(Guid.CreateVersion7()),
            warehouseItemId,
            warehouseInspectionId,
            sellerId,
            sellerAddressSnapshot,
            rejectionReason,
            nowUtc);
    }

    /// <summary>
    /// Raise the <see cref="WarehouseToSellerShipmentCreatedEvent"/> for consumption
    /// by notification handlers. Called by the inspection-rejected handler immediately
    /// after <see cref="Create"/> so the event lands in the same transactional outbox
    /// entry as the insert.
    /// </summary>
    public void MarkCreated()
    {
        RaiseDomainEvent(new WarehouseToSellerShipmentCreatedEvent(
            WarehouseToSellerShipmentId: Id.Value,
            WarehouseItemId:             WarehouseItemId.Value,
            SellerId:                    SellerId.Value,
            RejectionReason:             RejectionReason,
            OccurredAt:                  CreatedAt));
    }

    public UnitResult<e> MarkShipped(
        string providerCode,
        string trackingNumber,
        DateTime shippedAt,
        DateTime nowUtc)
    {
        if (Status != WarehouseToSellerShipmentStatus.Pending)
            return WarehouseErrors.WarehouseToSellerShipment.InvalidState;

        if (string.IsNullOrWhiteSpace(providerCode))
            return WarehouseErrors.WarehouseToSellerShipment.ProviderCodeRequired;

        if (string.IsNullOrWhiteSpace(trackingNumber))
            return WarehouseErrors.WarehouseToSellerShipment.TrackingNumberRequired;

        // Evidence guard — at least one PickupByWarehouseStaff photo required before shipping.
        if (!HasPickupEvidence)
            return e.Validation(
                "evidence",
                "WarehouseToSellerShipment.EvidenceRequired",
                "At least one pickup photo required before marking shipped.");

        ProviderCode   = providerCode;
        TrackingNumber = trackingNumber;
        ShippedAt      = shippedAt;
        Status         = WarehouseToSellerShipmentStatus.InTransit;
        ModifiedAt     = nowUtc;
        return UnitResult.Success<e>();
    }

    public UnitResult<e> MarkDelivered(DateTime deliveredAt, DateTime nowUtc)
    {
        if (Status != WarehouseToSellerShipmentStatus.InTransit)
            return WarehouseErrors.WarehouseToSellerShipment.InvalidState;

        DeliveredAt = deliveredAt;
        Status      = WarehouseToSellerShipmentStatus.Delivered;
        ModifiedAt  = nowUtc;
        return UnitResult.Success<e>();
    }

    public UnitResult<e> ConfirmBySeller(DateTime nowUtc)
    {
        if (Status != WarehouseToSellerShipmentStatus.Delivered)
            return WarehouseErrors.WarehouseToSellerShipment.InvalidState;

        // Evidence guard — at least one ReceiptBySeller photo required before confirming.
        if (!HasReceiptEvidence)
            return e.Validation(
                "evidence",
                "WarehouseToSellerShipment.EvidenceRequired",
                "At least one receipt photo required before confirming.");

        SellerConfirmedAt = nowUtc;
        Status            = WarehouseToSellerShipmentStatus.Closed;
        ModifiedAt        = nowUtc;
        return UnitResult.Success<e>();
    }

    public UnitResult<e> RecordDeliveryFailure(string reason, DateTime nowUtc)
    {
        if (Status != WarehouseToSellerShipmentStatus.InTransit
            && Status != WarehouseToSellerShipmentStatus.Delivered)
            return WarehouseErrors.WarehouseToSellerShipment.InvalidState;

        if (string.IsNullOrWhiteSpace(reason))
            return WarehouseErrors.WarehouseToSellerShipment.DeliveryFailureReasonRequired;

        DeliveryFailureReason = reason;
        Status                = WarehouseToSellerShipmentStatus.ReturnedToWarehouse;
        ModifiedAt            = nowUtc;
        return UnitResult.Success<e>();
    }

    /// <summary>
    /// Attaches an evidence photo to this shipment. Silent — does NOT raise a
    /// domain event per Principle 5 (V1 deliberately avoids events on evidence paths).
    /// </summary>
    /// <remarks>
    /// Allowed status windows:
    /// <list type="bullet">
    /// <item><see cref="WarehouseReturnEvidenceCategory.PickupByWarehouseStaff"/>: <see cref="WarehouseToSellerShipmentStatus.Pending"/> only.</item>
    /// <item><see cref="WarehouseReturnEvidenceCategory.ReceiptBySeller"/>: <see cref="WarehouseToSellerShipmentStatus.InTransit"/> or <see cref="WarehouseToSellerShipmentStatus.Delivered"/>.</item>
    /// </list>
    /// </remarks>
    public Result<WarehouseToSellerShipmentEvidence, e> AddEvidence(
        DateTime nowUtc,
        WarehouseReturnEvidenceCategory category,
        MediaUpload upload,
        UserId createdBy)
    {
        if (category == WarehouseReturnEvidenceCategory.PickupByWarehouseStaff)
        {
            if (Status != WarehouseToSellerShipmentStatus.Pending)
                return e.Conflict(
                    "WarehouseToSellerShipment.EvidenceNotAllowed",
                    $"Pickup evidence is only allowed in Pending status, but shipment is '{Status.Id}'.");
        }
        else if (category == WarehouseReturnEvidenceCategory.ReceiptBySeller)
        {
            if (Status != WarehouseToSellerShipmentStatus.InTransit
                && Status != WarehouseToSellerShipmentStatus.Delivered)
                return e.Conflict(
                    "WarehouseToSellerShipment.EvidenceNotAllowed",
                    $"Receipt evidence is only allowed in InTransit or Delivered, but shipment is '{Status.Id}'.");
        }
        else
        {
            return e.Validation(
                "category",
                "WarehouseToSellerShipment.UnknownEvidenceCategory",
                $"Unknown evidence category '{category.Id}'.");
        }

        var evidence = new WarehouseToSellerShipmentEvidence(
            WarehouseToSellerShipmentEvidenceId.From(Guid.CreateVersion7()),
            Id,
            category.Id,
            upload.Id,
            upload.Info.SecureUrl,
            upload.Info.FileName,
            upload.ResourceType,
            DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc),
            createdBy);

        _evidence.Add(evidence);
        ModifiedAt = nowUtc;
        return evidence;
    }

    /// <summary>
    /// Stamps the signed return-scoped QR token onto this shipment. Silent — no event
    /// per V1 explicit design (plan line 29). Called from the <see cref="MarkShipped"/>
    /// command handler after the shipment is successfully booked.
    /// </summary>
    public UnitResult<e> IssueReturnQr(string qrToken, DateTime nowUtc)
    {
        if (string.IsNullOrWhiteSpace(qrToken))
            return e.Validation("qrToken", "WarehouseToSellerShipment.InvalidQrToken", "QR token is required.");

        if (Status != WarehouseToSellerShipmentStatus.InTransit)
            return e.Conflict(
                "WarehouseToSellerShipment.InvalidState",
                $"QR token can only be issued in InTransit status, but shipment is '{Status.Id}'.");

        QrToken    = qrToken;
        ModifiedAt = nowUtc;
        return UnitResult.Success<e>();
    }
}
