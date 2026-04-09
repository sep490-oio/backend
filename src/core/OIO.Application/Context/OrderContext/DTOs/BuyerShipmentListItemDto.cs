namespace OIO.Application.Context.OrderContext.DTOs;

/// <summary>
/// Buyer-safe unified shipment row. Fed by <c>GetMyShipmentsQuery</c>, which
/// merges <see cref="SellerDirectShipmentListItemDto"/> and warehouse-booked
/// outbound shipments into a single paged feed for the buyer list UI.
/// <para>
/// <c>ShipmentKind</c> is the discriminator: <c>seller_direct</c> |
/// <c>warehouse_outbound</c>. All action flags are precomputed on the BE so
/// the buyer UI can gate CTAs without fetching per-row detail.
/// </para>
/// </summary>
public sealed record BuyerShipmentListItemDto(
    string ShipmentKind,
    Guid ShipmentId,
    Guid OrderId,
    string OrderNumber,
    string Status,
    string? ItemTitle,
    string? ItemImageUrl,
    string? CarrierName,
    string? CarrierTrackingNumber,
    string? InternalTrackingCode,
    DateTime? DecisionWindowEndsAt,
    bool CanAcknowledgeReceived,
    bool CanAccept,
    bool CanDispute,
    bool HasActiveDispute,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool QrAvailable,
    bool CanSubmitProof);
