using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.CatalogContext.Enums;

public sealed class ItemStatus : EnumValueObject<ItemStatus>
{
    public static readonly ItemStatus Draft = new("draft");
    public static readonly ItemStatus PendingVerify = new("pending_verify");
    public static readonly ItemStatus PendingReview = new("pending_review");
    public static readonly ItemStatus PendingConditionConfirmation = new("pending_condition_confirmation");
    public static readonly ItemStatus Approved = new("approved");
    public static readonly ItemStatus Rejected = new("rejected");
    public static readonly ItemStatus Active = new("active");
    public static readonly ItemStatus InAuction = new("in_auction");
    public static readonly ItemStatus Sold = new("sold");
    public static readonly ItemStatus Removed = new("removed");

    public ItemStatus(string id) : base(id) { }

    public bool IsEditable => this == Draft || this == Rejected;

    public bool CanTransitionTo(ItemStatus target)
    {
        return (this, target) switch
        {
            _ when this == Draft && target == PendingVerify => true,
            _ when this == Draft && target == PendingReview => true,
            // SECURITY: Draft → Active removed — items must pass moderation review (PendingVerify/PendingReview → Approved)
            // before becoming Active. Previously allowed sellers to bypass review via ActivateItemCommand.
            _ when this == Draft && target == Removed => true,
            _ when this == PendingVerify && target == Approved => true,
            _ when this == PendingVerify && target == Rejected => true,
            _ when this == PendingVerify && target == PendingConditionConfirmation => true,
            _ when this == PendingReview && target == Approved => true,
            _ when this == PendingReview && target == Rejected => true,
            _ when this == PendingConditionConfirmation && target == Approved => true,
            _ when this == PendingConditionConfirmation && target == Rejected => true,
            _ when this == Rejected && target == PendingVerify => true,
            _ when this == Rejected && target == PendingReview => true,
            _ when this == Rejected && target == Removed => true,
            _ when this == Approved && target == Active => true,
            _ when this == Approved && target == InAuction => true,
            _ when this == Approved && target == Removed => true,
            _ when this == Active && target == InAuction => true,
            _ when this == Active && target == Removed => true,
            _ when this == InAuction && target == Sold => true,
            _ when this == InAuction && target == Active => true,
            // Bug #11 fix: allow item to be removed even mid-auction
            // (physical item destroyed/lost, counterfeit confirmed, admin emergency).
            // Caller must ensure auction is also terminated/cancelled to avoid split-brain.
            _ when this == InAuction && target == Removed => true,
            _ when this == Sold && target == Removed => true,
            // Sold → Active: seller confirmed return receipt, item is back in stock
            // and eligible for re-listing. Driven by ConfirmOrderReturnReceivedCommandHandler.
            _ when this == Sold && target == Active => true,
            _ => false
        };
    }
}
