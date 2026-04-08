namespace OIO.Application.Context.WarehouseContext.DTOs;

/// <summary>
/// Row for the warehouse-staff outbound booking queue — one entry per paid/processing
/// order whose item lives in the platform warehouse (<c>warehouse_managed</c>) and
/// that has no active outbound shipment yet. Warehouse staff use this list to pick
/// the next order to book an outbound shipment for.
/// </summary>
public sealed record WarehouseStaffOutboundQueueItemDto(
    Guid OrderId,
    string OrderNumber,
    string OrderStatus,
    DateTime? OrderPaidAt,
    Guid AuctionId,
    Guid WarehouseItemId,
    string? ItemTitle,
    string? ItemPrimaryImageUrl,
    string? BuyerRecipientName,
    string? BuyerShippingAddress,
    string? SellerDisplayName);
