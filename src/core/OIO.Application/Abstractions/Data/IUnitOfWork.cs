using Microsoft.EntityFrameworkCore.Storage;

namespace OIO.Application.Abstractions.Data;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes <paramref name="action"/> inside a single retriable transaction.
    /// Wraps <c>Database.CreateExecutionStrategy().ExecuteAsync(...)</c> around
    /// <see cref="BeginTransactionAsync"/> so callers stay compatible with the
    /// configured Npgsql retrying execution strategy. The action receives the
    /// active transaction's cancellation token; commit happens on successful
    /// return, rollback on any thrown exception (including retries).
    /// </summary>
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}