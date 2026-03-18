using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class ReportStatus : EnumValueObject<ReportStatus>
{
    public static readonly ReportStatus Open = new("open");
    public static readonly ReportStatus UnderReview = new("under_review");
    public static readonly ReportStatus ActionTaken = new("action_taken");
    public static readonly ReportStatus Dismissed = new("dismissed");
    public static readonly ReportStatus Closed = new("closed");
    private ReportStatus(string id) : base(id) { }
}