using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using OIO.Application.Abstractions.Clock;
using OIO.Infrastructure.Settings;
using Quartz;

namespace OIO.Infrastructure.Outbox;

[DisallowConcurrentExecution]
internal sealed class OutboxCleanupJob(
    NpgsqlDataSource dataSource,
    ILogger<OutboxCleanupJob> logger,
    IOptionsMonitor<OutboxSettings> outboxSettingsOptions,
    IClock clock
) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var settings = outboxSettingsOptions.CurrentValue;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        // Pass 1: Successfully processed messages (existing behavior)
        var (successDeleted, successBatches) = await RunCleanupPass(
            connection, settings, cancellationToken,
            DeleteSuccessfulBatchAsync);

        // Pass 2: Poison messages (attempt_count >= MaxAttempts, processed_at IS NULL)
        await LogPoisonAuditIfNeeded(connection, settings, cancellationToken);
        var (poisonDeleted, poisonBatches) = await RunCleanupPass(
            connection, settings, cancellationToken,
            DeletePoisonBatchAsync);

        // Pass 3: Error messages (processed_at IS NOT NULL, error IS NOT NULL — deserialization failures)
        var (errorDeleted, errorBatches) = await RunCleanupPass(
            connection, settings, cancellationToken,
            DeleteErrorBatchAsync);

        // Logging
        if (successDeleted > 0)
        {
            OutboxCleanupLoggers.LogDeleted(logger, successDeleted, successBatches);
        }

        if (poisonDeleted > 0)
        {
            OutboxCleanupLoggers.LogPoisonDeleted(logger, poisonDeleted, poisonBatches);
        }

        if (errorDeleted > 0)
        {
            OutboxCleanupLoggers.LogErrorDeleted(logger, errorDeleted, errorBatches);
        }

        if (successDeleted == 0 && poisonDeleted == 0 && errorDeleted == 0)
        {
            OutboxCleanupLoggers.LogNoop(logger);
        }
    }

    private async Task<(int totalDeleted, int batchCount)> RunCleanupPass(
        NpgsqlConnection connection,
        OutboxSettings settings,
        CancellationToken cancellationToken,
        Func<NpgsqlConnection, OutboxSettings, IClock, CancellationToken, Task<int>> deleteBatch)
    {
        var state = (totalDeleted: 0, batchCount: 0);
        while (state.batchCount < settings.MaxCleanupBatchesPerRun)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var deleted = await deleteBatch(connection, settings, clock, cancellationToken);
            state = (state.totalDeleted + deleted, state.batchCount + 1);

            if (deleted < settings.CleanupBatchSize)
            {
                break;
            }

            if (settings.CleanupDelay > TimeSpan.Zero)
            {
                await Task.Delay(settings.CleanupDelay, cancellationToken);
            }
        }

        return state;
    }

    /// <summary>
    /// One-time forensic audit: logs the event types of poison messages before they are cleaned.
    /// Only runs when poison count exceeds a threshold to avoid noise.
    /// </summary>
    private async Task LogPoisonAuditIfNeeded(
        NpgsqlConnection connection,
        OutboxSettings settings,
        CancellationToken cancellationToken)
    {
        const string countSql = """
                                SELECT COUNT(*)
                                FROM outbox_messages
                                WHERE processed_at IS NULL
                                    AND attempt_count >= @MaxAttempts
                                    AND occurred_at < (@NowUtc - @Retention)
                                """;

        var poisonCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                commandText: countSql,
                parameters: new { settings.MaxAttempts, NowUtc = clock.UtcNow, Retention = settings.PoisonMessageRetention },
                cancellationToken: cancellationToken));

        if (poisonCount < 100)
        {
            return;
        }

        const string auditSql = """
                                 SELECT type, COUNT(*) AS count
                                 FROM outbox_messages
                                 WHERE processed_at IS NULL
                                     AND attempt_count >= @MaxAttempts
                                     AND occurred_at < (@NowUtc - @Retention)
                                 GROUP BY type
                                 ORDER BY count DESC
                                 LIMIT 20
                                 """;

        var auditResults = await connection.QueryAsync<(string Type, int Count)>(
            new CommandDefinition(
                commandText: auditSql,
                parameters: new { settings.MaxAttempts, NowUtc = clock.UtcNow, Retention = settings.PoisonMessageRetention },
                cancellationToken: cancellationToken));

        foreach (var (type, count) in auditResults)
        {
            OutboxCleanupLoggers.LogPoisonAudit(logger, type, count);
        }
    }

    // Pass 1: Successfully processed messages
    private static Task<int> DeleteSuccessfulBatchAsync(
        NpgsqlConnection connection,
        OutboxSettings settings,
        IClock clock,
        CancellationToken cancellationToken)
    {
        return connection.ExecuteAsync(
            new CommandDefinition(
                commandText:
                """
                WITH deleted AS (
                    SELECT id
                    FROM outbox_messages
                    WHERE processed_at IS NOT NULL
                        AND error IS NULL
                        AND occurred_at < (@NowUtc - @Retention)
                    ORDER BY occurred_at, id
                    LIMIT @BatchSize
                )
                DELETE FROM outbox_messages
                USING deleted
                WHERE outbox_messages.id = deleted.id;
                """,
                parameters: new
                {
                    Retention = settings.CleanupRetention,
                    BatchSize = settings.CleanupBatchSize,
                    NowUtc = clock.UtcNow
                },
                cancellationToken: cancellationToken
            )
        );
    }

    // Pass 2: Poison messages (permanently failed, never processed)
    private static Task<int> DeletePoisonBatchAsync(
        NpgsqlConnection connection,
        OutboxSettings settings,
        IClock clock,
        CancellationToken cancellationToken)
    {
        return connection.ExecuteAsync(
            new CommandDefinition(
                commandText:
                """
                WITH deleted AS (
                    SELECT id
                    FROM outbox_messages
                    WHERE processed_at IS NULL
                        AND attempt_count >= @MaxAttempts
                        AND occurred_at < (@NowUtc - @Retention)
                    ORDER BY occurred_at, id
                    LIMIT @BatchSize
                )
                DELETE FROM outbox_messages
                USING deleted
                WHERE outbox_messages.id = deleted.id;
                """,
                parameters: new
                {
                    settings.MaxAttempts,
                    Retention = settings.PoisonMessageRetention,
                    BatchSize = settings.CleanupBatchSize,
                    NowUtc = clock.UtcNow
                },
                cancellationToken: cancellationToken
            )
        );
    }

    // Pass 3: Error messages (deserialization failures — processed but with errors)
    private static Task<int> DeleteErrorBatchAsync(
        NpgsqlConnection connection,
        OutboxSettings settings,
        IClock clock,
        CancellationToken cancellationToken)
    {
        return connection.ExecuteAsync(
            new CommandDefinition(
                commandText:
                """
                WITH deleted AS (
                    SELECT id
                    FROM outbox_messages
                    WHERE processed_at IS NOT NULL
                        AND error IS NOT NULL
                        AND occurred_at < (@NowUtc - @Retention)
                    ORDER BY occurred_at, id
                    LIMIT @BatchSize
                )
                DELETE FROM outbox_messages
                USING deleted
                WHERE outbox_messages.id = deleted.id;
                """,
                parameters: new
                {
                    Retention = settings.CleanupRetention,
                    BatchSize = settings.CleanupBatchSize,
                    NowUtc = clock.UtcNow
                },
                cancellationToken: cancellationToken
            )
        );
    }
}

internal static partial class OutboxCleanupLoggers
{
    [LoggerMessage(Level = LogLevel.Information,
        Message = "Outbox cleanup deleted {DeletedCount} successfully processed messages in {BatchCount} batches.")]
    internal static partial void LogDeleted(ILogger logger, int deletedCount, int batchCount);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Outbox cleanup deleted {DeletedCount} poison messages in {BatchCount} batches.")]
    internal static partial void LogPoisonDeleted(ILogger logger, int deletedCount, int batchCount);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Outbox cleanup deleted {DeletedCount} error messages in {BatchCount} batches.")]
    internal static partial void LogErrorDeleted(ILogger logger, int deletedCount, int batchCount);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Outbox poison audit before cleanup: {EventType} = {Count} messages")]
    internal static partial void LogPoisonAudit(ILogger logger, string eventType, int count);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Outbox cleanup found no messages to delete.")]
    internal static partial void LogNoop(ILogger logger);
}
