using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class VerificationHistoryAction : EnumValueObject<VerificationHistoryAction>
{
    public static readonly VerificationHistoryAction Created = new("created");
    public static readonly VerificationHistoryAction Submitted = new("submitted");
    public static readonly VerificationHistoryAction AutoVerified = new("auto_verified");
    public static readonly VerificationHistoryAction ManualReviewStarted = new("manual_review_started");
    public static readonly VerificationHistoryAction Approved = new("approved");
    public static readonly VerificationHistoryAction Rejected = new("rejected");
    public static readonly VerificationHistoryAction Resubmitted = new("resubmitted");
    public static readonly VerificationHistoryAction Expired = new("expired");
    public static readonly VerificationHistoryAction Suspended = new("suspended");
    public static readonly VerificationHistoryAction DocumentUploaded = new("document_uploaded");
    public static readonly VerificationHistoryAction DocumentDeleted = new("document_deleted");
    public static readonly VerificationHistoryAction InfoUpdated = new("info_updated");
    public static readonly VerificationHistoryAction MovedToReview = new("moved_to_review");
    private VerificationHistoryAction(string id) : base(id) { }
}