using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.WarehouseContext.DTOs;

namespace OIO.Application.Context.OrderContext.DTOs;

/// <summary>
/// Lightweight row for the admin "all orders" list screen.
/// Joins an order with its auction item + buyer/seller display names
/// so the table renders without extra lookups.
/// </summary>
public sealed record AdminOrderListItemDto(
    Guid Id,
    string OrderNumber,
    Guid AuctionId,
    string Status,
    decimal TotalAmount,
    string Currency,
    string? BuyerDisplayName,
    string? SellerDisplayName,
    string? ItemTitle,
    string? ItemPrimaryImageUrl,
    DateTime CreatedAt,
    DateTime? PaidAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt);

/// <summary>
/// Full admin order detail — no buyer/seller ownership guard.
/// Includes order DTO, shipment, monitoring alerts, and escrow summary.
/// </summary>
public sealed record AdminOrderDetailDto(
    OrderDto Order,
    OutboundShipmentDto? OutboundShipment,
    IReadOnlyList<MonitoringAlertDto> MonitoringAlerts,
    AdminOrderEscrowSummaryDto? EscrowSummary);

public sealed record AdminOrderEscrowSummaryDto(
    decimal TotalHeld,
    decimal TotalReleased,
    decimal TotalRefunded,
    string Currency);
