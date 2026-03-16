namespace OIO.Application.Context.PaymentContext.DTOs;

public sealed record EscrowDto(
    Guid Id,
    Guid OrderId,
    Guid BuyerId,
    Guid SellerId,
    decimal Amount,
    string Currency,
    string Status,
    Guid? HoldTransactionId,
    DateTime CreatedAt,
    DateTime? ReleasedAt,
    DateTime? RefundedAt);
