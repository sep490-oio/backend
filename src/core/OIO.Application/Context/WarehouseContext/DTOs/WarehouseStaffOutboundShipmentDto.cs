namespace OIO.Application.Context.WarehouseContext.DTOs;

/// <summary>
/// Row for the warehouse-staff "Booked Shipments" tab — one per OutboundShipment
/// regardless of shipment mode. Enriched with the order / item / storage info
/// the FE needs to render a shipment card in the list.
/// </summary>
public sealed record WarehouseStaffOutboundShipmentListItemDto(
    Guid ShipmentId,
    Guid OrderId,
    string OrderNumber,
    string Status,
    string ShipmentMode,
    string ProviderCode,
    string? ExternalCarrierName,
    string? CarrierTrackingNumber,
    string? ItemTitle,
    string? ItemPrimaryImageUrl,
    string? StorageLocationLabel,
    string? RecipientName,
    DateTime CreatedAt,
    DateTime? DispatchedAt);

/// <summary>
/// Timeline entry projected from the shipment's audit trail. Sourced primarily
/// from <c>ShipmentTrackingEvent</c> rows (source = "carrier") with additional
/// "system" rows synthesized from the shipment's own lifecycle timestamps.
/// </summary>
public sealed record OutboundShipmentTimelineEventDto(
    string Code,
    string Label,
    DateTime OccurredAt,
    string Source,
    string? Note);

/// <summary>
/// Full detail payload for the warehouse-staff outbound shipment detail page.
/// Shipment-centric (not order-centric) — this is the screen staff open after
/// booking, or from the "Booked Shipments" tab.
/// </summary>
public sealed record WarehouseStaffOutboundShipmentDetailDto(
    // Shipment
    Guid ShipmentId,
    Guid OrderId,
    string OrderNumber,
    string Status,
    string ShipmentMode,
    string ProviderCode,
    string? ExternalCarrierName,
    string? CarrierTrackingNumber,
    string? ShippingLabelUrl,
    DateTime CreatedAt,
    DateTime? PackedAt,
    DateTime? DispatchedAt,
    DateTime? DeliveredAt,
    // Item
    Guid? WarehouseItemId,
    Guid ItemId,
    string? ItemTitle,
    string? ItemPrimaryImageUrl,
    string? StorageLocationLabel,
    // Recipient
    string? RecipientName,
    string? RecipientPhone,
    string? ComposedAddress,
    // Timeline + allowed actions
    IReadOnlyList<OutboundShipmentTimelineEventDto> Events,
    IReadOnlyList<string> AllowedManualStatuses,
    // QR token (external-carrier only; null for platform-managed)
    string? QrPayload,
    string? QrCodeUrl,
    int QrTokenVersion,
    DateTime? QrTokenIssuedAt,
    DateTime? QrTokenRevokedAt);

/// <summary>
/// Buyer-facing read-only shipment view surfaced by the QR deep-link. Minimal
/// fields — enough to render an order-scoped receipt page. Only populated for
/// external-carrier shipments.
/// </summary>
public sealed record BuyerOutboundShipmentDetailDto(
    Guid ShipmentId,
    Guid OrderId,
    string OrderNumber,
    string Status,
    string? ClientOrderCode,
    string? CarrierTrackingNumber,
    string? ExternalCarrierName,
    string? ItemTitle,
    string? ItemPrimaryImageUrl,
    string? RecipientName,
    string? ComposedAddress,
    DateTime? DispatchedAt,
    DateTime? DeliveredAt,
    bool QrAvailable,
    DateTime? BuyerReceivedPackageAt,
    DateTime? BuyerAcceptedAt,
    bool CanAcknowledgeReceived,
    bool CanAccept,
    bool CanOpenDispute,
    bool HasActiveDispute,
    DateTime? DecisionWindowEndsAt,
    bool CanSubmitProof,
    bool HasBuyerReceiptProof = false,
    bool CanSubmitReceiptProof = false,
    IReadOnlyList<EvidencePhotoDto>? PackagePhotos = null,
    IReadOnlyList<EvidencePhotoDto>? HandoverPhotos = null,
    IReadOnlyList<EvidencePhotoDto>? BuyerReceiptPhotos = null);

public sealed record EvidencePhotoDto(
    Guid Id,
    string? Url,
    DateTime CreatedAt);
