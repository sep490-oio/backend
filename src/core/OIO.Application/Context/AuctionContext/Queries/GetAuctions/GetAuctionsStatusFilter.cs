using System.Linq.Expressions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctions;

/// <summary>
/// Resolves the <c>WHERE auction.Status ...</c> predicate used by
/// <see cref="GetAuctionsQueryHandler"/>, with <c>StatusGroup</c>-first precedence.
/// Pulled out as a pure function so the precedence rules (dual-write, legacy exact-match,
/// "true All" default) can be unit-tested without an EF provider. The returned
/// <see cref="Expression"/> is consumed directly by EF's <c>Where</c>, so it must stay
/// composed of primitives EF can translate — no method calls to <c>IsSuccessfullyClosed</c>
/// or other computed helpers.
/// </summary>
public static class GetAuctionsStatusFilter
{
    /// <summary>
    /// Builds the status-axis predicate. Precedence:
    /// <list type="number">
    ///   <item><see paramref="statusGroup"/> is not <c>null</c> — coarse-grained group wins.</item>
    ///   <item>Legacy <see paramref="status"/> provided — exact-match on the enum id
    ///   (no transparent remap; <c>status=sold</c> returns only <see cref="AuctionStatus.Sold"/>).</item>
    ///   <item>Both null — "true All": excludes transient non-publishable states
    ///   (Draft, Pending, Approved, Ended).</item>
    /// </list>
    /// </summary>
    public static Expression<Func<Auction, bool>> Build(string? statusGroup, string? status)
    {
        if (statusGroup is not null)
        {
            return statusGroup switch
            {
                "active" => x => x.Status == AuctionStatus.Active,
                "scheduled" => x => x.Status == AuctionStatus.Scheduled,
                // Inlined IsSuccessfullyClosed so EF can translate the predicate.
                "sold" => x =>
                    x.Status == AuctionStatus.Sold ||
                    x.Status == AuctionStatus.Completed,
                "failed" => x =>
                    x.Status == AuctionStatus.Failed ||
                    x.Status == AuctionStatus.Cancelled ||
                    x.Status == AuctionStatus.Terminated ||
                    x.Status == AuctionStatus.PaymentDefaulted,
                // Empty string (or any other value that survived validation) → "true All".
                _ => NonTransient(),
            };
        }

        // TODO(058-F3-PR-2): remove this legacy exact-match branch once FE stops sending
        // `status=sold` during the dual-write compat window (see plan §4 F3 + ADR §7).
        if (!string.IsNullOrWhiteSpace(status) && AuctionStatus.Is(status))
        {
            var exact = AuctionStatus.FromId(status).Value;
            return x => x.Status == exact;
        }

        // Both StatusGroup and Status null/invalid → "true All" (excludes transient states).
        return NonTransient();
    }

    private static Expression<Func<Auction, bool>> NonTransient() =>
        x => x.Status != AuctionStatus.Draft &&
             x.Status != AuctionStatus.Pending &&
             x.Status != AuctionStatus.Approved &&
             x.Status != AuctionStatus.Ended;
}
