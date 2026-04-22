using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Application.Abstractions.Data;

/// <summary>
/// Serializes cross-aggregate writes that target a single auction inside the caller's
/// ambient transaction. Backed by Postgres <c>pg_advisory_xact_lock</c> so the lock
/// releases automatically at commit or rollback.
/// Both <c>AuctionCompletionOnOrderCompletedHandler</c> and <c>AuctionAutoCompleteJob</c>
/// call <see cref="AcquireAuctionLockAsync"/> as the first step inside their UoW to prevent
/// the event-handler and the safety-net job from racing on the same auction.
/// </summary>
public interface IAuctionLockRepository
{
    /// <summary>
    /// Acquires a transaction-scoped advisory lock keyed by the auction id.
    /// Blocks until the lock is granted; releases automatically when the ambient
    /// transaction commits or rolls back.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the caller has not opened an ambient transaction — a statement-level
    /// lock outside a tx releases immediately and would silently no-op.
    /// </exception>
    Task AcquireAuctionLockAsync(AuctionId auctionId, CancellationToken ct);
}
