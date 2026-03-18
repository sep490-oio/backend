using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ReviewContext.Enums;

public sealed class ReviewReportReason : EnumValueObject<ReviewReportReason>
{
    public static readonly ReviewReportReason Spam = new("spam");
    public static readonly ReviewReportReason Inappropriate = new("inappropriate");
    public static readonly ReviewReportReason Fake = new("fake");
    public static readonly ReviewReportReason Harassment = new("harassment");
    public static readonly ReviewReportReason Irrelevant = new("irrelevant");
    public static readonly ReviewReportReason Other = new("other");
    private ReviewReportReason(string id) : base(id) { }
}