namespace OIO.Application.Context.PaymentContext.DTOs;

public sealed record WalletSummaryDto(
    Guid WalletId,
    string Currency,
    decimal AvailableBalance,
    decimal PendingBalance,
    decimal TotalBalance,
    bool IsActive,
    DateTime UpdatedAt);
