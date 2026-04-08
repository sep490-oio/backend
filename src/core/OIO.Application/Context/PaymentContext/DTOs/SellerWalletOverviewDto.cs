namespace OIO.Application.Context.PaymentContext.DTOs;

public sealed record SellerWalletOverviewDto(
    decimal AvailableBalance,
    decimal PendingWithdrawalAmount,
    decimal EscrowHoldingAmount,
    decimal ReleasedToWalletAmount,
    string Currency,
    DateTime UpdatedAt);
