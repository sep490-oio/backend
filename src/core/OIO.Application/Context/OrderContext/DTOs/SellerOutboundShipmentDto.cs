namespace OIO.Application.Context.OrderContext.DTOs;

/// <summary>
/// Seller-scoped outbound shipment DTO. Shared by the seller list + detail
/// pages so both render identical context: which product, for which order,
/// shipped to which recipient.
///
/// This is intentionally a different type than the warehouse-generic
/// OutboundShipmentDto — seller pages must never be coupled to
/// warehouse-permissioned payloads.
/// </summary>
public sealed record SellerOutboundShipmentDto(
    Guid ShipmentId,
    Guid OrderId,
    string OrderNumber,
    string Status,
    string? ProviderCode,
    string? ProviderDisplayName,
    string? CarrierTrackingNumber,
    string ShipmentMode,
    DateTime CreatedAt,
    DateTime? ModifiedAt,
    DateTime? PackedAt,
    DateTime? DispatchedAt,
    DateTime? DeliveredAt,
    SellerOutboundShipmentItemDto? Item,
    SellerOutboundShipmentRecipientDto? Recipient,
    IReadOnlyList<SellerOutboundShipmentTrackingEventDto>? TrackingEvents);

public sealed record SellerOutboundShipmentItemDto(
    Guid ItemId,
    Guid AuctionId,
    string ItemTitle,
    string? PrimaryImageUrl,
    decimal FinalPrice,
    string Currency);

public sealed record SellerOutboundShipmentRecipientDto(
    string? RecipientName,
    string? PhoneNumber,
    string? ComposedAddress);

public sealed record SellerOutboundShipmentTrackingEventDto(
    string Status,
    string? Description,
    DateTime OccurredAt);
