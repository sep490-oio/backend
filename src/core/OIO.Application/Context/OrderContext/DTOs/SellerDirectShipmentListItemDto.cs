namespace OIO.Application.Context.OrderContext.DTOs;

/// <summary>
/// Seller-scoped list projection of a <c>SellerDirectShipment</c> row, enriched
/// with product + recipient context so the seller list page doesn't have to
/// round-trip orders/auctions per row. Distinct from <see cref="SellerDirectShipmentDto"/>
/// (the full detail projection) — this shape is optimized for card rendering.
/// </summary>
public sealed record SellerDirectShipmentListItemDto(
    Guid ShipmentId,
    string ShipmentIdDisplay,
    Guid OrderId,
    string OrderNumber,
    string InternalTrackingCode,
    string? ExternalCarrierName,
    string? ExternalTrackingCode,
    string Status,
    DateTime CreatedAt,
    DateTime? SellerDeclaredShippedAt,
    DateTime? DeliveredAt,
    DateTime? BuyerReceivedPackageAt,
    DateTime? BuyerAcceptedAt,
    bool ManualReviewRequired,
    ShipmentListItemItemDto? Item,
    ShipmentListItemRecipientDto? Recipient);

/// <summary>
/// Buyer-scoped list projection — extends the seller shape with the seller
/// display name, decision-window deadline, and precomputed action flags so
/// the buyer UI can gate CTAs without fetching the order.
/// </summary>
public sealed record MyDirectShipmentListItemDto(
    Guid ShipmentId,
    string ShipmentIdDisplay,
    Guid OrderId,
    string OrderNumber,
    string InternalTrackingCode,
    string? ExternalCarrierName,
    string? ExternalTrackingCode,
    string Status,
    DateTime CreatedAt,
    DateTime? SellerDeclaredShippedAt,
    DateTime? DeliveredAt,
    DateTime? BuyerReceivedPackageAt,
    DateTime? BuyerAcceptedAt,
    bool ManualReviewRequired,
    ShipmentListItemItemDto? Item,
    ShipmentListItemRecipientDto? Recipient,
    string? SellerDisplayName,
    DateTime? DecisionWindowEndsAt,
    bool CanSubmitProofOfDelivery,
    bool CanAccept,
    bool CanDispute);

public sealed record ShipmentListItemItemDto(
    Guid ItemId,
    string ItemTitle,
    string? PrimaryImageUrl,
    decimal FinalPrice,
    string Currency);

public sealed record ShipmentListItemRecipientDto(
    string? RecipientName,
    string? PhoneNumber,
    string? ComposedAddress);
