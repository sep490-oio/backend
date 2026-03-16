namespace OIO.Application.Context.OrderContext.DTOs;

public sealed record OrderDto(
    Guid Id,
    string OrderNumber,
    Guid AuctionId,
    Guid BuyerId,
    Guid SellerId,
    string Status,
    decimal TotalAmount,
    string Currency,
    DateTime CreatedAt,
    DateTime? PaymentDueAt,
    DateTime? PaidAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    DateTime? DecisionWindowEndsAt,
    DateTime? CompletedAt,
    DateTime? CancelledAt,
    string? EscrowStatus,
    string? TrackingNumber,
    OrderReturnDto? Return);
