namespace OIO.Application.Context.PaymentContext.DTOs;

/// <summary>
/// Seller finance transparency snapshot. Backend is the single source of
/// truth for fee math — frontend only formats/displays values. All decimal
/// figures are computed via <c>EscrowSettlementService.CalculateSellerSettlement</c>
/// using <c>SettlementOptions</c>; never hard-coded.
/// </summary>
public sealed record SellerFinanceOverviewDto(
    decimal WithdrawableBalance,
    decimal PendingWithdrawalAmount,
    decimal GrossEscrowHolding,
    decimal ReadyToReleaseAmount,
    decimal DisputedEscrowAmount,
    decimal EstimatedSellerNetPayout,
    decimal EstimatedPlatformCommission,
    decimal EstimatedInspectionFee,
    decimal PendingSellerFeeCharges,
    string Currency,
    DateTime UpdatedAt);
