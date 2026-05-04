namespace OIO.Application.Context.PaymentContext.DTOs;

/// <summary>
/// Wallet ledger row exposed to clients. Backward-compatible — original fields
/// retained, all new fields are nullable so existing clients keep working.
///
/// New fields (Phase 1 of /me/wallet revamp):
///   <list type="bullet">
///   <item><see cref="LedgerStatus"/>: ledger-side status: posted | pending | failed | reversed.
///       Always non-null. Falls back to <c>posted</c> for already-recorded ledger entries
///       that lack an underlying payment transaction.</item>
///   <item><see cref="SourceStatus"/>: status of the originating payment / withdrawal /
///       escrow row (nullable). Mirrors what was previously in <see cref="Status"/>
///       so existing UI keeps working.</item>
///   <item><see cref="EventType"/>: business event classification (e.g.
///       <c>wallet_top_up</c>, <c>auction_deposit_hold</c>, <c>auction_deposit_refund</c>,
///       <c>order_payment</c>, <c>order_refund</c>, <c>withdrawal_hold</c>,
///       <c>withdrawal_release</c>, <c>seller_payout</c>, <c>fee</c>).</item>
///   <item><see cref="ReasonCode"/>: i18n key the FE renders for the row's
///       human-readable label. Server never sends raw English copy.</item>
///   <item><see cref="ReferenceNumber"/> + <see cref="ReferenceTitle"/>: optional
///       extras for the reference chip (e.g. <c>ORD-...</c> + item title).</item>
///   </list>
/// </summary>
public sealed record WalletTransactionDto(
    Guid Id,
    string Type,
    decimal Amount,
    string Currency,
    string? Status,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string? Description,
    string? ReferenceType,
    Guid? ReferenceId,
    DateTime CreatedAt,
    string LedgerStatus,
    string? SourceStatus,
    string EventType,
    string ReasonCode,
    string? ReferenceNumber,
    string? ReferenceTitle);
