using System.Data;
using Dapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using OIO.Infrastructure.Outbox;

namespace OIO.Infrastructure.Scheduling.HostedServices;

/// <summary>
/// Plan B5a: <see cref="IHostedService"/> that drains any <c>TermsDocumentActivatedEvent</c>
/// outbox rows left unprocessed from a prior run, then polls periodically as a catch-up safety
/// net alongside the main <c>OutboxProcessor</c>.
///
/// <list type="bullet">
///   <item><description><see cref="StartAsync"/> drains existing unprocessed rows before entering the poll loop (plan AC: startup-drain test pins this ordering).</description></item>
///   <item><description>Periodic loop runs every 60 seconds to catch any activation events that were inserted but somehow missed by the primary Quartz-scheduled OutboxProcessor.</description></item>
///   <item><description>Publishing is delegated to <c>IPublisher</c> inside a new DI scope per message — this re-triggers <c>TermsDocumentActivatedEventHandler</c> which already performs idempotent fan-out (500 per batch, via <c>IdempotentDomainEventHandler&lt;T&gt;</c> decorator keyed on EventId).</description></item>
/// </list>
/// </summary>
internal sealed class TermsNotificationOutboxProcessor : BackgroundService
{
    private const string TermsActivatedEventTypeName =
        "OIO.Domain.Context.UserContext.Aggregates.Users.Events.TermsDocumentActivatedEvent";

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

    private readonly NpgsqlDataSource _dataSource;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOutboxMessageResolver _outboxMessageResolver;
    private readonly ILogger<TermsNotificationOutboxProcessor> _logger;

    public TermsNotificationOutboxProcessor(
        NpgsqlDataSource dataSource,
        IServiceScopeFactory scopeFactory,
        IOutboxMessageResolver outboxMessageResolver,
        ILogger<TermsNotificationOutboxProcessor> logger)
    {
        _dataSource = dataSource;
        _scopeFactory = scopeFactory;
        _outboxMessageResolver = outboxMessageResolver;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        // Drain any pre-existing unprocessed rows BEFORE the poll loop begins.
        // This is the AC pinned by the B5a startup-drain test.
        try
        {
            var drained = await DrainOnceAsync(cancellationToken);
            if (drained > 0)
            {
                _logger.LogInformation(
                    "metric=terms_notification_startup_drained_total Count={Count}: re-driven pre-existing TermsDocumentActivatedEvent outbox rows on boot.",
                    drained);
            }
        }
        catch (Exception ex)
        {
            // Startup must not block the host; the poll loop will retry.
            _logger.LogError(ex,
                "TermsNotificationOutboxProcessor startup-drain failed; will retry on the poll interval.");
        }

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollInterval, stoppingToken);
                await DrainOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "TermsNotificationOutboxProcessor poll iteration failed; continuing.");
            }
        }
    }

    /// <summary>
    /// Selects up to <see cref="OutboxConstants.MaxAttemptsIndexFilter"/>-capped unprocessed
    /// <c>TermsDocumentActivatedEvent</c> rows, publishes each via <see cref="IPublisher"/>
    /// (which re-invokes <c>TermsDocumentActivatedEventHandler</c> — idempotent by EventId),
    /// and marks them processed. Returns the number of rows drained.
    /// </summary>
    private async Task<int> DrainOnceAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string selectSql = """
                                 SELECT id, type, content
                                 FROM outbox_messages
                                 WHERE processed_at IS NULL
                                   AND attempt_count < @MaxAttempts
                                   AND type = @Type
                                 ORDER BY occurred_at
                                 LIMIT 50
                                 FOR NO KEY UPDATE SKIP LOCKED
                                 """;

        var rows = (await connection.QueryAsync<OutboxRow>(
            new CommandDefinition(
                commandText: selectSql,
                parameters: new
                {
                    MaxAttempts = OutboxConstants.MaxAttemptsIndexFilter,
                    Type = TermsActivatedEventTypeName,
                },
                transaction: transaction,
                cancellationToken: cancellationToken))).ToList();

        if (rows.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return 0;
        }

        var nowUtc = DateTime.UtcNow;
        var processed = 0;
        foreach (var row in rows)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var deserialized = _outboxMessageResolver.DeserializeEvent(row.Type, row.Content);
            if (deserialized.IsFailure)
            {
                _logger.LogError(
                    "TermsNotificationOutboxProcessor failed to deserialize outbox row {OutboxId}: {Error}",
                    row.Id, deserialized.Error.Message);
                continue;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
                await publisher.Publish(deserialized.Value, cancellationToken);

                const string markSql = """
                                       UPDATE outbox_messages
                                       SET processed_at = @ProcessedAt, error = NULL
                                       WHERE id = @Id AND processed_at IS NULL
                                       """;
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        commandText: markSql,
                        parameters: new { Id = row.Id, ProcessedAt = nowUtc },
                        transaction: transaction,
                        cancellationToken: cancellationToken));
                processed++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "TermsNotificationOutboxProcessor failed to publish outbox row {OutboxId}; leaving unprocessed for retry.",
                    row.Id);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return processed;
    }

    private sealed record OutboxRow(Guid Id, string Type, string Content);
}
