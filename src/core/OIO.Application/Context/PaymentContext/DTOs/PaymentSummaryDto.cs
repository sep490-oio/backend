namespace OIO.Application.Context.PaymentContext.DTOs;

public sealed record PaymentSummaryDto(
    int CompletedPayments,
    int FailedPayments,
    int WalletTopUps,
    int WithdrawalPendingCount,
    decimal WithdrawalPendingTotal,
    int HoldingEscrowCount,
    decimal HoldingEscrowTotal,
    decimal ReleasedEscrowTotal,
    decimal RefundedEscrowTotal,
    decimal TotalRevenue,
    decimal TotalSystemBalance);
