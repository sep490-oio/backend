namespace OIO.Application.Context.PaymentContext.DTOs;

/// <summary>
/// One row of the seller escrow ledger. Backend computes all amounts (fees,
/// net payouts, hold reasons) so the frontend remains a pure formatter.
/// </summary>
public sealed record SellerEscrowLedgerRowDto(
    Guid OrderId,
    string OrderNumber,
    Guid? AuctionId,
    string ItemTitle,
    decimal GrossPaidAmount,
    string Currency,
    string OrderStatus,
    string EscrowStatus,
    string? HoldReason,
    DateTime? BuyerPaidAt,
    DateTime? ExpectedReleaseAt,
    DateTime? DecisionWindowEndsAt,
    bool IsPlatformVerifiedItem,
    decimal PlatformCommissionAmount,
    decimal InspectionFeeAmount,
    decimal EstimatedNetPayout,
    decimal? ActualReleasedAmount,
    Guid? DisputeId,
    string? DisputeStatus);
