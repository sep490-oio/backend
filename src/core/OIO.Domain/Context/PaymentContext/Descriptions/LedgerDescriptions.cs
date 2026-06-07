using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OIO.Domain.Context.PaymentContext.Descriptions;

// ──────────────────────────────────────────────────────────────────────────────
//  Single source of truth for Transaction / WalletTransaction descriptions.
//
//  The "purpose" of a ledger entry is marked through its Description string, which
//  is BOTH human-readable AND parsed by several read-side queries (event-type
//  classification, idempotency, money attribution). Historically every call site
//  hand-rolled its own `$"..."` string, producing four+ incompatible formats and
//  several latent casing/marker mismatches.
//
//  Every writer now builds descriptions through <see cref="LedgerDescriptions"/>,
//  and every reader matches against the <see cref="LedgerTags"/> /
//  <see cref="LedgerMarkers"/> constants, so the two can never drift again.
//
//  CANONICAL FORMAT:  "[Tag] Summary - Key: Value - Key: Value"
//    • Tag      — PascalCase purpose marker, see <see cref="LedgerTags"/>.
//    • Summary  — concise English phrase; intentionally contains the prose
//                 markers (<see cref="LedgerMarkers"/>) that read-side filters key
//                 off (e.g. "Withdrawal", "Escrow", "inspection", "commission").
//    • Metadata — zero or more " - Key: Value" pairs (<see cref="LedgerKeys"/>);
//                 empty values are skipped.
//
//  Load-bearing tokens deliberately preserved for backward compatibility with
//  rows already persisted under the old formats:
//    • "AuctionId: {guid}"          (PaymentReadModelMapper.ParseAuctionId)
//    • "for auction {guid}"         (PaymentReadModelMapper.ParseAuctionIdFromSuffix)
//    • "(inspection {guid})"        (FE SellerWalletPage regex)
//    • "Auction winner deposit applied" (OrderMappings)
//    • "[HybridHold] Wallet portion committed" (OrderMappings)
//    • prose words: Withdrawal, Escrow, wallet top-up, inspection, commission,
//      platform, forfeit, penalty
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Canonical PascalCase bracket tags. Shared by writers and the read-side parsers
/// so a description's purpose marker is defined in exactly one place.
/// </summary>
public static class LedgerTags
{
    public const string AuctionDeposit = "AuctionDeposit";
    public const string AuctionBuyNow = "AuctionBuyNow";
    public const string AuctionBuyNowDepositApplied = "AuctionBuyNowDepositApplied";
    public const string OrderPayment = "OrderPayment";
    public const string WalletTopUp = "WalletTopUp";
    public const string HybridHold = "HybridHold";
    public const string Refund = "Refund";
    public const string Payout = "Payout";
    public const string Fee = "Fee";
    public const string Withdrawal = "Withdrawal";
    public const string LinkCard = "LinkCard";
    public const string Cancelled = "Cancelled";

    /// <summary>Renders a tag as its bracketed prefix, e.g. "[AuctionDeposit]".</summary>
    public static string Bracket(string tag) => $"[{tag}]";
}

/// <summary>Canonical metadata key names used in the " - Key: Value" suffix.</summary>
public static class LedgerKeys
{
    public const string Order = "Order";              // human-readable order number
    public const string OrderId = "OrderId";          // order GUID
    public const string AuctionId = "AuctionId";      // auction GUID (load-bearing: ParseAuctionId)
    public const string ReservationId = "ReservationId";
    public const string Escrow = "Escrow";            // escrow GUID
    public const string TxnRef = "TxnRef";
    public const string Amount = "Amount";
    public const string WalletPortion = "WalletPortion";
    public const string Reason = "Reason";
    public const string Bank = "Bank";
    public const string Holder = "Holder";
    public const string Item = "Item";
}

/// <summary>
/// Prose substrings that read-side filters match on. Centralised here so writer
/// summaries and reader predicates reference the same literal.
/// </summary>
public static class LedgerMarkers
{
    public const string Withdrawal = "Withdrawal";
    public const string Escrow = "Escrow";
    public const string AuctionDeposit = "Auction deposit";
    public const string WalletTopUp = "wallet top-up";
    public const string Fee = "Fee";
    public const string Commission = "commission";
    public const string Platform = "platform";
    public const string Inspection = "inspection";
    public const string Forfeit = "forfeit";
    public const string Penalty = "penalty";
    public const string Compensation = "Compensation";
    public const string WinnerDepositApplied = "Auction winner deposit applied";
    public const string HybridWalletPortionCommitted = "Wallet portion committed";
    public const string AuctionIdMarker = "AuctionId:";
    public const string ForAuctionMarker = "for auction ";
}

/// <summary>
/// Builds canonical ledger descriptions. All Transaction / WalletTransaction
/// descriptions in the codebase are produced here.
/// </summary>
public static class LedgerDescriptions
{
    /// <summary>
    /// Core formatter: "[tag] summary" followed by " - Key: Value" for every
    /// metadata pair whose value is non-empty.
    /// </summary>
    public static string Compose(string tag, string summary, params (string Key, string? Value)[] meta)
    {
        var sb = new StringBuilder();
        sb.Append('[').Append(tag).Append("] ").Append(summary);
        foreach (var (key, value) in meta)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;
            sb.Append(" - ").Append(key).Append(": ").Append(value);
        }
        return sb.ToString();
    }

    private static string Money(decimal amount) => amount.ToString("N0", CultureInfo.InvariantCulture);
    private static string Percent(decimal rate) => rate.ToString("P0", CultureInfo.InvariantCulture);
    private static string G(Guid id) => id.ToString();

    private static readonly Regex GuidPattern = new(
        "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
        RegexOptions.Compiled);

    /// <summary>
    /// Sanitizes USER-SUPPLIED free text (bank name, account holder, item title, reason) before it
    /// is embedded in a description that is later substring-parsed. Strips bracket tags (so a value
    /// can't forge a <see cref="LedgerTags"/> prefix), collapses GUIDs (so it can't forge the
    /// id-based money attribution in admin queries or the <c>AuctionId:</c>/<c>for auction</c>
    /// parsers), and bounds length. Never applied to the structured GUID/order-number metadata,
    /// which are system-controlled and load-bearing.
    /// </summary>
    private static string Safe(string? value, int maxLength = 120)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        // Neutralize BEFORE bounding so a GUID straddling the length cap can't leave a fragment.
        var s = value.Trim();
        s = s.Replace('[', '(').Replace(']', ')');
        s = GuidPattern.Replace(s, "<id>");
        if (s.Length > maxLength)
            s = s[..maxLength];
        return s;
    }

    // ── VNPay gateway transaction (CreateVnPayPaymentUrl) ─────────────────────
    // Tag is resolved from PaymentPurpose at the call site (Application layer)
    // because PaymentPurpose lives in Application and cannot be referenced here.

    /// <summary>
    /// Gateway-initiated transaction. <paramref name="tag"/> is one of
    /// <see cref="LedgerTags.AuctionDeposit"/>/<see cref="LedgerTags.OrderPayment"/>/
    /// <see cref="LedgerTags.AuctionBuyNow"/>/<see cref="LedgerTags.WalletTopUp"/>.
    /// </summary>
    public static string Gateway(string tag, string detail, Guid? auctionId = null, Guid? reservationId = null)
        => Compose(
            tag,
            detail,
            (LedgerKeys.AuctionId, auctionId?.ToString()),
            (LedgerKeys.ReservationId, reservationId?.ToString()));

    // ── Order payment (wallet) ────────────────────────────────────────────────
    public static string WalletOrderPayment(Guid orderId)
        => Compose(LedgerTags.OrderPayment, "Order payment from wallet", (LedgerKeys.OrderId, G(orderId)));

    public static string BuyNowFullyCoveredByDeposit(Guid orderId)
        => Compose(LedgerTags.OrderPayment, "Buy-now order fully covered by deposit", (LedgerKeys.OrderId, G(orderId)));

    /// <summary>Wallet debit settling the net order amount. Marker-free human memo.</summary>
    public static string WalletPaymentForOrder(Guid orderId)
        => Compose(LedgerTags.OrderPayment, "Wallet payment", (LedgerKeys.OrderId, G(orderId)));

    /// <summary>
    /// Winner auction-deposit applied to an order. Preserves the
    /// <see cref="LedgerMarkers.WinnerDepositApplied"/> marker that OrderMappings reads.
    /// </summary>
    public static string WinnerDepositApplied(Guid orderId)
        => Compose(LedgerTags.AuctionDeposit, LedgerMarkers.WinnerDepositApplied, (LedgerKeys.OrderId, G(orderId)));

    // ── Hybrid wallet+VNPay hold lifecycle ────────────────────────────────────
    public static string HybridHold(Guid orderId, decimal walletPortion)
        => Compose(LedgerTags.HybridHold, "Hold for hybrid payment",
            (LedgerKeys.OrderId, G(orderId)), (LedgerKeys.WalletPortion, Money(walletPortion)));

    public static string HybridHoldRollback(Guid orderId)
        => Compose(LedgerTags.HybridHold, "Rollback hold for failed hybrid payment", (LedgerKeys.OrderId, G(orderId)));

    /// <summary>Preserves the "Wallet portion committed" marker (OrderMappings).</summary>
    public static string HybridHoldCommitted(Guid orderId)
        => Compose(LedgerTags.HybridHold, LedgerMarkers.HybridWalletPortionCommitted, (LedgerKeys.OrderId, G(orderId)));

    public static string HybridHoldReleased(Guid orderId)
        => Compose(LedgerTags.HybridHold, "Released after failed VNPay payment", (LedgerKeys.OrderId, G(orderId)));

    // ── Buy-now deposit funding ───────────────────────────────────────────────
    /// <summary>Transaction memo. Preserves the "[AuctionBuyNowDepositApplied]" tag (OrderMappings) and "AuctionId:" marker.</summary>
    public static string BuyNowDepositApplied(Guid auctionId, Guid reservationId, Guid orderId)
        => Compose(LedgerTags.AuctionBuyNowDepositApplied, "Auction buy-now deposit applied",
            (LedgerKeys.AuctionId, G(auctionId)),
            (LedgerKeys.ReservationId, G(reservationId)),
            (LedgerKeys.OrderId, G(orderId)));

    /// <summary>Wallet-side memo for the buy-now deposit debit.</summary>
    public static string BuyNowDepositAppliedToWallet(Guid reservationId)
        => Compose(LedgerTags.AuctionBuyNowDepositApplied, "Auction buy-now deposit applied",
            (LedgerKeys.ReservationId, G(reservationId)));

    // ── Auction deposit (VNPay credit + hold) ─────────────────────────────────
    public static string VnPayAuctionDepositCredit(string txnRef)
        => Compose(LedgerTags.AuctionDeposit, "VNPay auction deposit", (LedgerKeys.TxnRef, txnRef));

    /// <summary>Preserves the "Auction deposit" marker (ResolveReference).</summary>
    public static string AuctionDepositHold(string txnRef)
        => Compose(LedgerTags.AuctionDeposit, "Auction deposit hold", (LedgerKeys.TxnRef, txnRef));

    /// <summary>
    /// Wallet hold for a deposit funded directly from wallet balance. Preserves the
    /// "Auction deposit" marker AND the "for auction {guid}" suffix (ParseAuctionIdFromSuffix).
    /// </summary>
    public static string AuctionDepositFromWallet(Guid auctionId)
        => $"{LedgerTags.Bracket(LedgerTags.AuctionDeposit)} Auction deposit from wallet {LedgerMarkers.ForAuctionMarker}{G(auctionId)}";

    public static string ReturnedAuctionDeposit(string reason, Guid auctionId)
        => $"{LedgerTags.Bracket(LedgerTags.AuctionDeposit)} Returned auction deposit - {LedgerKeys.Reason}: {Safe(reason)} {LedgerMarkers.ForAuctionMarker}{G(auctionId)}";

    public static string ForfeitedAuctionDeposit(string reason)
        => Compose(LedgerTags.Fee, "Forfeited auction deposit", (LedgerKeys.Reason, Safe(reason)));

    public static string AutoBidReservationReleased(Guid auctionId, string reason)
        => $"{LedgerTags.Bracket(LedgerTags.AuctionDeposit)} Released auto-bid reservation {LedgerMarkers.ForAuctionMarker}{G(auctionId)} - {LedgerKeys.Reason}: {Safe(reason)}";

    public static string AutoBidCancelled(string auctionId)
        => $"{LedgerTags.Bracket(LedgerTags.AuctionDeposit)} Auto-bid cancelled by user {LedgerMarkers.ForAuctionMarker}{auctionId}";

    public static string AutoBidReservation(string auctionId)
        => $"{LedgerTags.Bracket(LedgerTags.AuctionDeposit)} Auto-bid reservation {LedgerMarkers.ForAuctionMarker}{auctionId}";

    public static string AutoBidReservationAdjusted(string auctionId)
        => $"{LedgerTags.Bracket(LedgerTags.AuctionDeposit)} Auto-bid reservation adjusted {LedgerMarkers.ForAuctionMarker}{auctionId}";

    // ── Wallet top-up ─────────────────────────────────────────────────────────
    /// <summary>Preserves the "wallet top-up" marker (ResolveEventType / ResolveReference).</summary>
    public static string WalletTopUpCredit(string txnRef)
        => Compose(LedgerTags.WalletTopUp, "VNPay wallet top-up", (LedgerKeys.TxnRef, txnRef));

    public static string LateBuyNowCredit(string txnRef)
        => Compose(LedgerTags.OrderPayment, "Late buy-now payment credited to wallet", (LedgerKeys.TxnRef, txnRef));

    // ── Withdrawal lifecycle ──────────────────────────────────────────────────
    public static string WithdrawalRequest(decimal amount, string bankName, string accountHolder)
        => Compose(LedgerTags.Withdrawal, "Withdrawal request",
            (LedgerKeys.Amount, Money(amount)), (LedgerKeys.Bank, Safe(bankName)), (LedgerKeys.Holder, Safe(accountHolder)));

    public static string WithdrawalHold(decimal amount)
        => Compose(LedgerTags.Withdrawal, "Withdrawal hold", (LedgerKeys.Amount, Money(amount)));

    public static string WithdrawalRejectedUnhold(decimal amount)
        => Compose(LedgerTags.Withdrawal, "Withdrawal rejected - funds unheld", (LedgerKeys.Amount, Money(amount)));

    public static string WithdrawalCancelledUnhold(decimal amount)
        => Compose(LedgerTags.Withdrawal, "Withdrawal cancelled - funds unheld", (LedgerKeys.Amount, Money(amount)));

    public static string WithdrawalCompleted(decimal amount)
        => Compose(LedgerTags.Withdrawal, "Withdrawal completed", (LedgerKeys.Amount, Money(amount)));

    // ── Escrow settlement (EscrowSettlementService) ───────────────────────────
    public static string EscrowReleaseForOrder(string orderNumber, string reason)
        => Compose(LedgerTags.Payout, "Escrow release", (LedgerKeys.Order, orderNumber), (LedgerKeys.Reason, Safe(reason)));

    public static string EscrowRefundForOrder(string orderNumber, string reason)
        => Compose(LedgerTags.Refund, "Escrow refund", (LedgerKeys.Order, orderNumber), (LedgerKeys.Reason, Safe(reason)));

    public static string PlatformCommissionForOrder(string orderNumber)
        => Compose(LedgerTags.Fee, "Platform commission", (LedgerKeys.Order, orderNumber));

    public static string PlatformCommissionForPartialPayout(string orderNumber)
        => Compose(LedgerTags.Fee, "Platform commission for partial payout", (LedgerKeys.Order, orderNumber));

    public static string PartialPayoutAfterRefundForOrder(string orderNumber)
        => Compose(LedgerTags.Payout, "Partial payout after refund", (LedgerKeys.Order, orderNumber));

    public static string OfflineInspectionFee(string orderNumber)
        => Compose(LedgerTags.Fee, "Offline inspection fee", (LedgerKeys.Order, orderNumber));

    public static string OfflineInspectionFeeForPartialPayout(string orderNumber)
        => Compose(LedgerTags.Fee, "Offline inspection fee for partial payout", (LedgerKeys.Order, orderNumber));

    public static string BuyerWinDisputeInspectionFee(string orderNumber, string reason)
        => Compose(LedgerTags.Fee, "Buyer-win dispute inspection fee", (LedgerKeys.Order, orderNumber), (LedgerKeys.Reason, Safe(reason)));

    public static string DisputeCommissionCharge(string orderNumber, string reason)
        => Compose(LedgerTags.Fee, "Platform commission charged to seller for buyer-win dispute",
            (LedgerKeys.Order, orderNumber), (LedgerKeys.Reason, Safe(reason)));

    public static string DisputeCommissionFromSeller(string orderNumber)
        => Compose(LedgerTags.Fee, "Platform commission from seller for buyer-win dispute", (LedgerKeys.Order, orderNumber));

    /// <summary>Preserves the "(inspection {guid})" suffix consumed by the FE wallet-page regex.</summary>
    public static string InspectionFeeForRejectedItem(string itemTitle, Guid inspectionId)
        => $"{LedgerTags.Bracket(LedgerTags.Fee)} Inspection fee for rejected item \"{Safe(itemTitle)}\" (inspection {G(inspectionId)}).";

    public static string PendingFeeCollection()
        => Compose(LedgerTags.Fee, "Pending fee collection");

    // ── Escrow admin / direct commands ────────────────────────────────────────
    public static string AdminForceForfeitEscrow(string orderNumber, string reason)
        => Compose(LedgerTags.Fee, "Admin force forfeit escrow to platform",
            (LedgerKeys.Order, orderNumber), (LedgerKeys.Reason, Safe(reason)));

    public static string AdminForceRefundEscrow(string orderNumber, string reason)
        => Compose(LedgerTags.Refund, "Admin force refund escrow to buyer",
            (LedgerKeys.Order, orderNumber), (LedgerKeys.Reason, Safe(reason)));

    public static string AdminForceReleaseEscrow(string orderNumber, string reason)
        => Compose(LedgerTags.Payout, "Admin force release escrow to seller",
            (LedgerKeys.Order, orderNumber), (LedgerKeys.Reason, Safe(reason)));

    public static string EscrowForfeitToPlatform(string orderNumber, string reason)
        => Compose(LedgerTags.Fee, "Escrow forfeit to platform",
            (LedgerKeys.Order, orderNumber), (LedgerKeys.Reason, Safe(reason)));

    public static string EscrowRefundedToBuyer(string orderNumber, string reason)
        => Compose(LedgerTags.Refund, "Escrow refunded to buyer",
            (LedgerKeys.Order, orderNumber), (LedgerKeys.Reason, Safe(reason)));

    public static string EscrowReleasedToSeller(string orderNumber, string reason)
        => Compose(LedgerTags.Payout, "Escrow released to seller",
            (LedgerKeys.Order, orderNumber), (LedgerKeys.Reason, Safe(reason)));

    public static string EscrowReleasePayout(Guid orderId)
        => Compose(LedgerTags.Payout, "Escrow release payout", (LedgerKeys.OrderId, G(orderId)));

    public static string EscrowRefund(Guid orderId, decimal? partialAmount)
        => Compose(LedgerTags.Refund, partialAmount.HasValue ? "Partial escrow refund" : "Full escrow refund",
            (LedgerKeys.OrderId, G(orderId)),
            (LedgerKeys.Amount, partialAmount.HasValue ? Money(partialAmount.Value) : null));

    public static string PartialPayoutAfterPartialRefund(Guid orderId)
        => Compose(LedgerTags.Payout, "Partial payout after partial refund", (LedgerKeys.OrderId, G(orderId)));

    public static string PayoutFromEscrow(Guid escrowId)
        => Compose(LedgerTags.Payout, "Payout from escrow", (LedgerKeys.Escrow, G(escrowId)));

    public static string RefundFromEscrow(Guid escrowId)
        => Compose(LedgerTags.Refund, "Refund from escrow", (LedgerKeys.Escrow, G(escrowId)));

    public static string PartialPayoutFromEscrow(Guid escrowId)
        => Compose(LedgerTags.Payout, "Partial payout from escrow", (LedgerKeys.Escrow, G(escrowId)));

    // ── Cancelled auction payment penalties ───────────────────────────────────
    public static string DepositPenalty(decimal penaltyRate)
        => Compose(LedgerTags.Fee, $"Deposit penalty ({Percent(penaltyRate)}) for cancelled auction payment");

    public static string PartialDepositReturn(decimal returnRate)
        => Compose(LedgerTags.AuctionDeposit, $"Partial deposit return ({Percent(returnRate)}) after cancelled auction payment");

    // ── Card linking ──────────────────────────────────────────────────────────
    public static string LinkPaymentCard()
        => Compose(LedgerTags.LinkCard, "Link VNPay payment card");

    // ── Cancelled transaction suffix (Transaction.CancelPending) ───────────────
    /// <summary>Suffix appended to an existing description when a pending tx is cancelled.</summary>
    public static string CancelledSuffix(string reason)
        => $"{LedgerTags.Bracket(LedgerTags.Cancelled)} {LedgerKeys.Reason}: {Safe(reason)}";
}
