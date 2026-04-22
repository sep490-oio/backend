using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Infrastructure.Persistence.Repositories;

/// <summary>
/// Postgres advisory-lock implementation of <see cref="IAuctionLockRepository"/>.
/// Uses <c>pg_advisory_xact_lock(hashtext(@key))</c> so the lock is scoped to the
/// ambient transaction and released automatically on commit/rollback.
/// </summary>
public sealed class AuctionLockRepository : IAuctionLockRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AuctionLockRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task AcquireAuctionLockAsync(AuctionId auctionId, CancellationToken ct)
    {
        if (_dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "AcquireAuctionLockAsync requires an ambient transaction — " +
                "pg_advisory_xact_lock releases at statement boundary outside a tx.");
        }

        var key = $"auction:{auctionId.Value}";
        return _dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtext({key}))",
            ct);
    }
}
