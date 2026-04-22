using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.Aggregates.Disputes;
using OIO.Domain.Context.ModerationContext.Enums;

namespace OIO.Application.Context.ModerationContext.Services;

/// <summary>
/// Shared read helpers for dispute state checks that multiple auction-completion
/// code paths rely on. Keeping this in one place prevents silent drift: if a new
/// <see cref="DisputeStatus"/> is added, both the event handler and the
/// <c>AuctionAutoCompleteJob</c> update together.
/// </summary>
public static class DisputeStateQueries
{
    public static Task<bool> HasOpenDisputeForAuctionAsync(
        IDbContext dbContext,
        AuctionId auctionId,
        CancellationToken cancellationToken)
    {
        return dbContext.Set<Dispute>()
            .AsNoTracking()
            .AnyAsync(d =>
                    d.AuctionId == auctionId &&
                    d.Status != DisputeStatus.Resolved &&
                    d.Status != DisputeStatus.Closed &&
                    d.Status != DisputeStatus.Cancelled &&
                    d.Status != DisputeStatus.Rejected,
                cancellationToken);
    }
}
