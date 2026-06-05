namespace OIO.Application.Context.PaymentContext.DTOs;

public sealed record PendingSellerFeeDto(
    Guid TransactionId,
    string TransactionNumber,
    decimal Amount,
    string Currency,
    string? Description,
    DateTime CreatedAt,
    Guid? OrderId,
    Guid? AuctionId
);
