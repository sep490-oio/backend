using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class DisputeStatus : EnumValueObject<DisputeStatus>
{
    public static readonly DisputeStatus Draft = new("draft");
    public static readonly DisputeStatus Open = new("open");
    public static readonly DisputeStatus UnderReview = new("under_review");
    public static readonly DisputeStatus AwaitingResponse = new("awaiting_response");
    public static readonly DisputeStatus Escalated = new("escalated");
    public static readonly DisputeStatus Resolved = new("resolved");
    public static readonly DisputeStatus Closed = new("closed");
    public static readonly DisputeStatus Cancelled = new("cancelled");
    private DisputeStatus(string id) : base(id) { }
}