using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.WarehouseContext.DTOs;

namespace OIO.Application.Context.OrderContext.DTOs;

/// <summary>
/// Row in the admin "completed auctions" screen. Joins a sold/buy-now auction
/// with its order + derived payment/fulfillment status so the admin table can
/// render without extra lookups.
/// </summary>
public sealed record AdminCompletedAuctionListItemDto(
    Guid AuctionId,
    string ItemTitle,
    string? ItemPrimaryImageUrl,
    Guid? WinnerId,
    string? WinnerDisplayName,
    Guid SellerId,
    string? SellerDisplayName,
    decimal FinalPrice,
    string Currency,
    Guid OrderId,
    string OrderNumber,
    string OrderStatus,
    // pending_payment | paid | payment_overdue
    string PaymentStatus,
    // seller_self_ship | warehouse_managed
    string FulfillmentFlow,
    // awaiting_seller_ship | warehouse_outbound_pending | picked_up | on_delivering
    // | delivered | shipping_overdue | escalated
    string FulfillmentStatus,
    DateTime? PaymentDueAt,
    DateTime? PaidAt,
    DateTime? ShipByAt,
    bool IsShippingOverdue,
    DateTime? EscalatedAt,
    string? EscalationReason,
    DateTime CreatedAt);

public sealed record AdminCompletedAuctionDetailDto(
    AdminCompletedAuctionListItemDto Summary,
    OrderDto Order,
    OutboundShipmentDto? OutboundShipment,
    IReadOnlyList<MonitoringAlertDto> MonitoringAlerts);
