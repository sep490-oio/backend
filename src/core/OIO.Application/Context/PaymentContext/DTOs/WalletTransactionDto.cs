namespace OIO.Application.Context.PaymentContext.DTOs;

public sealed record WalletTransactionDto(
    Guid Id,
    string Type,
    decimal Amount,
    string Currency,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string? Description,
    string? ReferenceType,
    Guid? ReferenceId,
    DateTime CreatedAt);
