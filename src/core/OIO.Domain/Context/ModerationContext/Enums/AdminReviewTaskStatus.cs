using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class AdminReviewTaskStatus : EnumValueObject<AdminReviewTaskStatus>
{
    public static readonly AdminReviewTaskStatus Open = new("open");
    public static readonly AdminReviewTaskStatus InProgress = new("in_progress");
    public static readonly AdminReviewTaskStatus Completed = new("completed");
    public static readonly AdminReviewTaskStatus Cancelled = new("cancelled");
    public static readonly AdminReviewTaskStatus Overdue = new("overdue");
    private AdminReviewTaskStatus(string id) : base(id) { }
}