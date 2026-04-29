namespace OIO.Application.Context.ModerationContext.DTOs;

/// <summary>
/// Read-side projection of dispute filing eligibility for a single (caller, target) pair.
/// Returned by <c>GET /api/disputes/eligibility?targetType=...&amp;entityId=...</c>.
/// FE consumes this advisory payload to hide/show the report button and filter
/// domain/caseType dropdowns. BE remains the sole authority at submit time.
/// </summary>
/// <param name="CanReport">
/// False → FE hides the report button. <see cref="Reason"/> explains why.
/// </param>
/// <param name="Role">
/// Resolved role key for the caller against the target, or null when not a participant.
/// Mirrors <c>DisputeEligibilityRule.Role*</c> constants
/// (e.g. "seller", "winner", "losing_bidder", "observer", "buyer", "owner").
/// </param>
/// <param name="AllowedDomains">Domain ids the caller may file under (may be empty).</param>
/// <param name="AllowedCaseTypes">
/// Map keyed by domain id (every key appears in <see cref="AllowedDomains"/>) to the
/// list of caseType ids allowed under that domain for the resolved role/target.
/// </param>
/// <param name="Reason">
/// Enum-style string when <see cref="CanReport"/> is false. One of:
/// "NotAuthenticated" | "NotParticipant" | "AuctionStillActive" | "AuctionPreBidding" | "OutOfScope".
/// Null when <see cref="CanReport"/> is true.
/// </param>
public sealed record DisputeEligibilityDto(
    bool CanReport,
    string? Role,
    IReadOnlyList<string> AllowedDomains,
    IReadOnlyDictionary<string, IReadOnlyList<string>> AllowedCaseTypes,
    string? Reason);
