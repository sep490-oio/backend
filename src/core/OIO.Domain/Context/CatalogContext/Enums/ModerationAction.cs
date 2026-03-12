using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.CatalogContext.Enums;

public sealed class ModerationAction : EnumValueObject<ModerationAction>
{
    public static readonly ModerationAction Submitted = new("submitted");
    public static readonly ModerationAction Assigned = new("assigned");
    public static readonly ModerationAction StartedReview = new("started_review");
    public static readonly ModerationAction Approved = new("approved");
    public static readonly ModerationAction Rejected = new("rejected");
    public static readonly ModerationAction Resubmitted = new("resubmitted");
    public static readonly ModerationAction Removed = new("removed");
    private ModerationAction(string id) : base(id) { }
}