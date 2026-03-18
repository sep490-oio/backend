using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ReviewContext.Enums;

public sealed class ReviewReportStatus : EnumValueObject<ReviewReportStatus>
{
    public static readonly ReviewReportStatus Pending = new("pending");
    public static readonly ReviewReportStatus Reviewed = new("reviewed");
    public static readonly ReviewReportStatus ActionTaken = new("action_taken");
    public static readonly ReviewReportStatus Dismissed = new("dismissed");
    private ReviewReportStatus(string id) : base(id) { }
}