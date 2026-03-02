using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.UserContext.Enums;

public sealed class SellerKycHistoryAction : EnumValueObject<SellerKycHistoryAction>
{
    public static SellerKycHistoryAction Created = new("created");
    public static SellerKycHistoryAction Submitted = new("submitted");
    public static SellerKycHistoryAction AutoVerified = new("auto_verified");
    public static SellerKycHistoryAction ManualReviewStarted = new("manual_review_started");
    public static SellerKycHistoryAction Approved = new("approved");
    public static SellerKycHistoryAction Rejected = new("rejected");
    public static SellerKycHistoryAction ReSubmitted = new("resubmitted");
    public static SellerKycHistoryAction Expired = new("expired");
    public static SellerKycHistoryAction Suspended = new("suspended");
    public static SellerKycHistoryAction DocumentUploaded = new("document_uploaded");
    public static SellerKycHistoryAction DocumentDeleted = new("document_deleted");
    public static SellerKycHistoryAction InfoUpdated = new("info_updated");
    
    public SellerKycHistoryAction(string id) : base(id) {}
}