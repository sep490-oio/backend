namespace OIO.Application.Context.ModerationContext.Services;

/// <summary>
/// Application-side orchestrator for dispute eligibility.
/// Resolves the caller's role against a target entity (DB-backed) and checks the
/// auction-only timing gate. Does NOT know the eligibility matrix — that lives in
/// <c>OIO.Domain.Context.ModerationContext.DisputeEligibilityRule</c>.
/// </summary>
public interface IDisputeEligibilityService
{
    /// <summary>
    /// Returns the caller's role key for the given target, or null when not a participant.
    /// Role keys mirror <c>DisputeEligibilityRule.Role*</c> constants
    /// (e.g. "seller", "winner", "losing_bidder", "observer", "buyer", "owner").
    /// </summary>
    Task<string?> ResolveRoleAsync(Guid userId, string targetType, Guid entityId, CancellationToken ct);

    /// <summary>
    /// True when the target is in a state where filing a dispute is allowed.
    /// For auctions this enforces <c>DisputeEligibilityRule.AuctionAllowedStatuses</c>;
    /// for other targets the gate is currently always open.
    /// </summary>
    Task<bool> IsTimingAllowedAsync(string targetType, Guid entityId, CancellationToken ct);
}
