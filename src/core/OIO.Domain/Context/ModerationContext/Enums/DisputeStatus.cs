using CSharpFunctionalExtensions;

namespace OIO.Domain.Context.ModerationContext.Enums;

public sealed class DisputeStatus : EnumValueObject<DisputeStatus>
{
    public static readonly DisputeStatus Open = new("open");
    public static readonly DisputeStatus AwaitingRespondent = new("awaiting_respondent");
    public static readonly DisputeStatus AwaitingEvidence = new("awaiting_evidence");
    public static readonly DisputeStatus UnderReview = new("under_review");
    public static readonly DisputeStatus AwaitingInternalReview = new("awaiting_internal_review");
    public static readonly DisputeStatus AwaitingResolutionApproval = new("awaiting_resolution_approval");
    public static readonly DisputeStatus Resolved = new("resolved");
    public static readonly DisputeStatus Rejected = new("rejected");
    public static readonly DisputeStatus Closed = new("closed");
    public static readonly DisputeStatus Cancelled = new("cancelled");
    private DisputeStatus(string id) : base(id) { }

    private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new()
    {
        ["open"] = ["awaiting_respondent", "awaiting_evidence", "under_review", "cancelled"],
        ["awaiting_respondent"] = ["awaiting_evidence", "under_review", "cancelled"],
        ["awaiting_evidence"] = ["under_review", "awaiting_internal_review", "cancelled"],
        ["under_review"] = ["awaiting_internal_review", "awaiting_resolution_approval", "resolved", "rejected"],
        ["awaiting_internal_review"] = ["awaiting_resolution_approval", "under_review"],
        ["awaiting_resolution_approval"] = ["resolved", "rejected", "under_review"],
        ["resolved"] = [],
        ["rejected"] = [],
        ["cancelled"] = [],
    };

    public bool CanTransitionTo(DisputeStatus target)
        => AllowedTransitions.TryGetValue(Id, out var targets) && targets.Contains(target.Id);

    public bool IsTerminal
        => Id is "resolved" or "rejected" or "cancelled";
}