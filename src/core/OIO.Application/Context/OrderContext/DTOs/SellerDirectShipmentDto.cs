namespace OIO.Application.Context.OrderContext.DTOs;

/// <summary>
/// API projection of <c>SellerDirectShipment</c>. All timestamps are UTC.
/// Mirrors the aggregate 1:1 so the FE can render the timeline + status pill
/// and gate progression buttons without a second request.
/// </summary>
/// <param name="QrPayload">Canonical payload/deep-link string to encode into a QR image client-side.</param>
/// <param name="QrCodeUrl">Legacy/optional. DO NOT assume this is an image URL. FE must render QR from <see cref="QrPayload"/>.</param>
public sealed record SellerDirectShipmentDto(
    Guid Id,
    Guid OrderId,
    string ShipmentIdDisplay,
    string InternalTrackingCode,
    string QrPayload,
    string QrCodeUrl,
    string? ExternalCarrierName,
    string? ExternalTrackingCode,
    string Status,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    DateTime? CarrierBookedAt,
    DateTime? PickedUpAt,
    DateTime? OnDeliveringAt,
    DateTime? DeliveredAt,
    DateTime? BuyerReceivedPackageAt,
    DateTime? BuyerAcceptedAt,
    DateTime? DisputedAt,
    DateTime? CompletedAt,
    DateTime? SellerDeclaredShippedAt,
    IReadOnlyList<ShipmentEvidenceDto> SellerPackagePhotos,
    IReadOnlyList<ShipmentEvidenceDto> SellerHandoverProofs,
    IReadOnlyList<ShipmentEvidenceDto> BuyerDeliveryPhotos,
    string? BuyerPackageCondition,
    string? BuyerConditionNotes,
    bool ManualReviewRequired,
    string? ManualReviewReason,
    int QrTokenVersion,
    DateTime? QrTokenIssuedAt,
    DateTime? QrTokenRevokedAt);

/// <summary>
/// Projection of a single <c>SellerDirectShipmentEvidence</c> entity. Kind is
/// implied by which list on <see cref="SellerDirectShipmentDto"/> owns this row.
/// </summary>
public sealed record ShipmentEvidenceDto(
    Guid Id,
    Guid MediaUploadId,
    string MediaUrl,
    DateTime CreatedAt);
