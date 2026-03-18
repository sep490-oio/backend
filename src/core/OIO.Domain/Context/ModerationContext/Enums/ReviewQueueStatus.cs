using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class ReviewQueueStatus : EnumValueObject<ReviewQueueStatus>
{
    public static readonly ReviewQueueStatus Open = new("open");
    public static readonly ReviewQueueStatus Assigned = new("assigned");
    public static readonly ReviewQueueStatus InProgress = new("in_progress");
    public static readonly ReviewQueueStatus Completed = new("completed");
    public static readonly ReviewQueueStatus Cancelled = new("cancelled");
    private ReviewQueueStatus(string id) : base(id) { }
}