namespace OIO.Application.Context.PaymentContext.DTOs;

public sealed record PaymentTransactionDto(
    Guid Id,
    string TransactionNumber,
    Guid UserId,
    string? UserDisplayName,
    Guid? OrderId,
    string? OrderNumber,
    Guid? AuctionId,
    string? AuctionItemTitle,
    string Type,
    decimal Amount,
    decimal Fee,
    decimal NetAmount,
    string Currency,
    string Status,
    string? GatewayProvider,
    string? Description,
    DateTime CreatedAt,
    DateTime? ProcessedAt);
