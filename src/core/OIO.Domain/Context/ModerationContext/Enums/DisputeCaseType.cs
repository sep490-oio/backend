using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class DisputeCaseType : EnumValueObject<DisputeCaseType>
{
    public static readonly DisputeCaseType WinnerNonPayment = new("winner_non_payment");
    public static readonly DisputeCaseType SellerNonFulfillment = new("seller_non_fulfillment");
    public static readonly DisputeCaseType AuctionCancelledAfterCommitment = new("auction_cancelled_after_commitment");
    public static readonly DisputeCaseType BuyNowCommitmentIssue = new("buy_now_commitment_issue");
    public static readonly DisputeCaseType DepositForfeitOrRefundIssue = new("deposit_forfeit_or_refund_issue");
    public static readonly DisputeCaseType InspectionDisagreement = new("inspection_disagreement");
    public static readonly DisputeCaseType ConditionMismatch = new("condition_mismatch");
    public static readonly DisputeCaseType AuthenticityConcern = new("authenticity_concern");
    public static readonly DisputeCaseType NotAsDescribedAfterDelivery = new("not_as_described_after_delivery");
    public static readonly DisputeCaseType WarehouseDamage = new("warehouse_damage");
    public static readonly DisputeCaseType WalletTopupMissing = new("wallet_topup_missing");
    public static readonly DisputeCaseType DuplicateCharge = new("duplicate_charge");
    public static readonly DisputeCaseType DepositNotApplied = new("deposit_not_applied");
    public static readonly DisputeCaseType BuyNowDepositOffsetError = new("buy_now_deposit_offset_error");
    public static readonly DisputeCaseType RefundMissing = new("refund_missing");
    public static readonly DisputeCaseType PayoutMismatch = new("payout_mismatch");
    public static readonly DisputeCaseType PackageNotReceived = new("package_not_received");
    public static readonly DisputeCaseType DamagedPackage = new("damaged_package");
    public static readonly DisputeCaseType WrongItemReceived = new("wrong_item_received");
    public static readonly DisputeCaseType MissingItems = new("missing_items");
    public static readonly DisputeCaseType CarrierMarkedDeliveredButNotReceived = new("carrier_marked_delivered_but_not_received");
    public static readonly DisputeCaseType HandoverEvidenceConflict = new("handover_evidence_conflict");
    private DisputeCaseType(string id) : base(id) { }
}
