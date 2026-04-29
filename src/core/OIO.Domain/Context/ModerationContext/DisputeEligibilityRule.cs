using System.Collections.Immutable;

namespace OIO.Domain.Context.ModerationContext;

/// <summary>
/// Pure-function eligibility matrix. Domain primitive — no DI, no DB.
/// Source of truth for who (role) can file what dispute (domain + caseType)
/// against what target (auction/order/payment/shipment/warehouse_item).
/// Enforced as aggregate invariant inside <c>Dispute.CreateCase</c>.
/// Also queried by Application layer to build read-side eligibility DTO.
/// </summary>
public static class DisputeEligibilityRule
{
    // ── Role keys (string for FE/JSON parity) ──
    public const string RoleSeller = "seller";
    public const string RoleWinner = "winner";
    public const string RoleLosingBidder = "losing_bidder";
    public const string RoleObserver = "observer";
    public const string RoleBuyer = "buyer";
    public const string RoleOwner = "owner"; // payment

    // ── Target types ──
    public const string TargetAuction = "auction";
    public const string TargetOrder = "order";
    public const string TargetPayment = "payment";
    public const string TargetShipment = "shipment";
    public const string TargetWarehouseItem = "warehouse_item";

    // ── Domain keys (mirror DisputeDomain.Id values) ──
    private const string DomainAuctionSettlement = "auction_settlement";
    private const string DomainItemCondition = "item_condition";
    private const string DomainPayment = "payment";
    private const string DomainShipping = "shipping";

    // ── CaseType keys (mirror DisputeCaseType.Id values) ──
    private const string CtWinnerNonPayment = "winner_non_payment";
    private const string CtSellerNonFulfillment = "seller_non_fulfillment";
    private const string CtBuyNowCommitmentIssue = "buy_now_commitment_issue";
    private const string CtAuctionCancelledAfterCommitment = "auction_cancelled_after_commitment";
    private const string CtDepositForfeitOrRefundIssue = "deposit_forfeit_or_refund_issue";
    private const string CtInspectionDisagreement = "inspection_disagreement";
    private const string CtConditionMismatch = "condition_mismatch";
    private const string CtAuthenticityConcern = "authenticity_concern";
    private const string CtNotAsDescribedAfterDelivery = "not_as_described_after_delivery";
    private const string CtWarehouseDamage = "warehouse_damage";
    private const string CtWalletTopupMissing = "wallet_topup_missing";
    private const string CtDuplicateCharge = "duplicate_charge";
    private const string CtDepositNotApplied = "deposit_not_applied";
    private const string CtBuyNowDepositOffsetError = "buy_now_deposit_offset_error";
    private const string CtRefundMissing = "refund_missing";
    private const string CtPayoutMismatch = "payout_mismatch";
    private const string CtPackageNotReceived = "package_not_received";
    private const string CtDamagedPackage = "damaged_package";
    private const string CtWrongItemReceived = "wrong_item_received";
    private const string CtMissingItems = "missing_items";
    private const string CtCarrierMarkedDeliveredButNotReceived = "carrier_marked_delivered_but_not_received";
    private const string CtHandoverEvidenceConflict = "handover_evidence_conflict";

    /// <summary>
    /// Auction-only timing gate.
    /// Whitelist = post-bidding terminal-or-near-terminal states where dispute is meaningful.
    /// Excluded: <c>draft, pending, approved, scheduled</c> (no participant data yet)
    /// and <c>active</c> (still bidding — disputes premature).
    /// Verified against <c>AuctionStatus.cs</c> (12 values total).
    /// </summary>
    public static readonly IReadOnlySet<string> AuctionAllowedStatuses
        = new HashSet<string>(StringComparer.Ordinal)
        {
            "ended",
            "sold",
            "completed",
            "payment_defaulted",
            "cancelled",
            "failed",
            "terminated",
        };

    // Matrix keyed by (target, role) → domain → caseTypes
    private static readonly IReadOnlyDictionary<
        (string Target, string Role),
        IReadOnlyDictionary<string, IReadOnlyList<string>>> _matrix
        = BuildMatrix();

    /// <summary>
    /// True iff the (role, target, domain, caseType) tuple appears in the matrix.
    /// All four dimensions must match — otherwise false (default-deny).
    /// </summary>
    public static bool IsAllowed(string roleKey, string targetType, string domain, string caseType)
    {
        if (!_matrix.TryGetValue((targetType, roleKey), out var domainMap)) return false;
        if (!domainMap.TryGetValue(domain, out var allowedCaseTypes)) return false;
        return allowedCaseTypes.Contains(caseType, StringComparer.Ordinal);
    }

    /// <summary>
    /// Returns the list of domain keys allowed for (role, target).
    /// Empty list when no matrix entry exists.
    /// </summary>
    public static IReadOnlyList<string> AllowedDomainsFor(string roleKey, string targetType)
        => _matrix.TryGetValue((targetType, roleKey), out var dm)
            ? dm.Keys.ToImmutableArray()
            : ImmutableArray<string>.Empty;

    /// <summary>
    /// Returns the list of caseType keys allowed for (role, target, domain).
    /// Empty list when no matrix entry exists.
    /// </summary>
    public static IReadOnlyList<string> AllowedCaseTypesFor(string roleKey, string targetType, string domain)
        => _matrix.TryGetValue((targetType, roleKey), out var dm) && dm.TryGetValue(domain, out var ct)
            ? ct
            : ImmutableArray<string>.Empty;

    private static IReadOnlyDictionary<
        (string Target, string Role),
        IReadOnlyDictionary<string, IReadOnlyList<string>>> BuildMatrix()
    {
        var matrix = new Dictionary<
            (string Target, string Role),
            IReadOnlyDictionary<string, IReadOnlyList<string>>>
        {
            // ── Auction ──
            [(TargetAuction, RoleSeller)] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [DomainAuctionSettlement] = ImmutableArray.Create(
                    CtWinnerNonPayment,
                    CtBuyNowCommitmentIssue,
                    CtAuctionCancelledAfterCommitment),
            },

            [(TargetAuction, RoleWinner)] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [DomainAuctionSettlement] = ImmutableArray.Create(
                    CtWinnerNonPayment,
                    CtSellerNonFulfillment,
                    CtBuyNowCommitmentIssue,
                    CtAuctionCancelledAfterCommitment,
                    CtDepositForfeitOrRefundIssue),
                [DomainItemCondition] = ImmutableArray.Create(
                    CtInspectionDisagreement,
                    CtConditionMismatch,
                    CtAuthenticityConcern,
                    CtNotAsDescribedAfterDelivery,
                    CtWarehouseDamage),
                [DomainPayment] = ImmutableArray.Create(
                    CtWalletTopupMissing,
                    CtDuplicateCharge,
                    CtDepositNotApplied,
                    CtBuyNowDepositOffsetError,
                    CtRefundMissing,
                    CtPayoutMismatch),
                [DomainShipping] = ImmutableArray.Create(
                    CtPackageNotReceived,
                    CtDamagedPackage,
                    CtWrongItemReceived,
                    CtMissingItems,
                    CtCarrierMarkedDeliveredButNotReceived,
                    CtHandoverEvidenceConflict),
            },

            [(TargetAuction, RoleLosingBidder)] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [DomainAuctionSettlement] = ImmutableArray.Create(
                    CtDepositForfeitOrRefundIssue,
                    CtAuctionCancelledAfterCommitment),
            },

            // (TargetAuction, RoleObserver) — no entry → IsAllowed always false → canReport=false.

            // ── Order ──
            [(TargetOrder, RoleBuyer)] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [DomainItemCondition] = ImmutableArray.Create(
                    CtInspectionDisagreement,
                    CtConditionMismatch,
                    CtAuthenticityConcern,
                    CtNotAsDescribedAfterDelivery),
                [DomainShipping] = ImmutableArray.Create(
                    CtPackageNotReceived,
                    CtDamagedPackage,
                    CtWrongItemReceived,
                    CtMissingItems,
                    CtCarrierMarkedDeliveredButNotReceived,
                    CtHandoverEvidenceConflict),
                [DomainPayment] = ImmutableArray.Create(
                    CtDuplicateCharge,
                    CtRefundMissing),
            },

            [(TargetOrder, RoleSeller)] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [DomainPayment] = ImmutableArray.Create(
                    CtPayoutMismatch,
                    CtWalletTopupMissing),
                [DomainAuctionSettlement] = ImmutableArray.Create(
                    CtWinnerNonPayment),
            },

            // ── Payment / Shipment / WarehouseItem (BE-only hardening, no FE entry today) ──
            [(TargetPayment, RoleOwner)] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [DomainPayment] = ImmutableArray.Create(
                    CtWalletTopupMissing,
                    CtDuplicateCharge,
                    CtDepositNotApplied,
                    CtBuyNowDepositOffsetError,
                    CtRefundMissing,
                    CtPayoutMismatch),
            },

            [(TargetShipment, RoleBuyer)] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [DomainShipping] = ImmutableArray.Create(
                    CtPackageNotReceived,
                    CtDamagedPackage,
                    CtWrongItemReceived,
                    CtMissingItems,
                    CtCarrierMarkedDeliveredButNotReceived,
                    CtHandoverEvidenceConflict),
                [DomainItemCondition] = ImmutableArray.Create(
                    CtNotAsDescribedAfterDelivery,
                    CtWarehouseDamage),
            },

            [(TargetWarehouseItem, RoleSeller)] = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
            {
                [DomainItemCondition] = ImmutableArray.Create(
                    CtWarehouseDamage,
                    CtConditionMismatch),
            },
        };

        return matrix;
    }
}
