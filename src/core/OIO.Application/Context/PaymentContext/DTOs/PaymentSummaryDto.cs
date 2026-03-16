namespace OIO.Application.Context.PaymentContext.DTOs;

public sealed record PaymentSummaryDto(
    int CompletedPayments,
    int FailedPayments,
    int WalletTopUps,
    int WithdrawalPendingCount,
    int HoldingEscrowCount,
    decimal ReleasedEscrowTotal,
    decimal RefundedEscrowTotal);
