using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Text.Json;
using Dapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using OIO.Application.Abstractions.Clock;
using OIO.Infrastructure.Settings;

namespace OIO.Infrastructure.Outbox;

internal sealed class OutboxProcessor
{
    private static readonly JsonSerializerOptions SnakeCaseNamingJsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly ILogger<OutboxProcessor> _logger;
    private readonly IOptionsMonitor<OutboxSettings> _outboxSettings;
    private readonly NpgsqlDataSource _dataSource;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOutboxMessageResolver _outboxMessageResolver;
    private readonly IClock _clock;

    public OutboxProcessor(
        ILogger<OutboxProcessor> logger,
        IOptionsMonitor<OutboxSettings> outboxSettings,
        NpgsqlDataSource dataSource,
        IServiceScopeFactory scopeFactory,
        IOutboxMessageResolver outboxMessageResolver,
        IClock clock)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _outboxSettings = outboxSettings ?? throw new ArgumentNullException(nameof(outboxSettings));
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _outboxMessageResolver =
            outboxMessageResolver ?? throw new ArgumentNullException(nameof(outboxMessageResolver));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    private const int MaxParallelism = 5;

    public async Task Execute(CancellationToken cancellationToken = default)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var stepStopwatch = new Stopwatch();

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);


        // 1. Query unprocessed outbox messages
        stepStopwatch.Restart();
        var messages = await GetOutboxMessagesAsync(connection, transaction, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var queryTime = stepStopwatch.ElapsedMilliseconds;

        if (messages.Count == 0)
        {
            OutboxMessagesProcessorLoggers.LogNoMessagesToProcess(_logger);
            return;
        }

        // 2. Publish messages concurrently
        stepStopwatch.Restart();
        var updateQueue = new ConcurrentQueue<OutboxUpdate>();
        await Parallel.ForEachAsync(
            messages,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxParallelism,
                CancellationToken = cancellationToken
            },
            async (message, token) =>
            {
                // Each message gets its own DI scope to avoid DbContext concurrency issues.
                // Notification handlers resolved from scoped IPublisher each get an isolated DbContext.
                using var scope = _scopeFactory.CreateScope();
                var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
                await PublishMessage(message,
                    updateQueue,
                    publisher,
                    _clock,
                    _outboxMessageResolver,
                    _logger,
                    token);
            }
        );
        var publishTime = stepStopwatch.ElapsedMilliseconds;

        
        stepStopwatch.Restart();
        var rowsAffected = updateQueue switch
        {
            { IsEmpty: false } =>
                await MarkMessagesAsProcessed(connection,
                    transaction,
                    updateQueue,
                    cancellationToken),
            _ => 0
        };
        cancellationToken.ThrowIfCancellationRequested();
        var updateTime = stepStopwatch.ElapsedMilliseconds;

        // 4. Commit transaction
        await transaction.CommitAsync(cancellationToken);

        totalStopwatch.Stop();

        // Only log at Info when slow (>500ms) or many messages; otherwise Debug
        if (totalStopwatch.ElapsedMilliseconds > 500 || messages.Count > 10)
        {
            _logger.LogWarning(
                "⚠️ Outbox slow: {TotalTime}ms, {MessageCount} messages (query={QueryTime}ms, publish={PublishTime}ms, update={UpdateTime}ms)",
                totalStopwatch.ElapsedMilliseconds, messages.Count, queryTime, publishTime, updateTime);
        }
        else
        {
            OutboxMessagesProcessorLoggers.LogProcessingPerformance(
                logger: _logger,
                totalTime: totalStopwatch.ElapsedMilliseconds,
                queryTime: queryTime,
                publishTime: publishTime,
                updateTime: updateTime,
                messageCount: messages.Count,
                rowsAffected: rowsAffected
            );
        }
    }
    
    private async Task<IReadOnlyList<OutboxMessage>> GetOutboxMessagesAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        const string sql = $"""                
                            SELECT id, type, content
                            FROM outbox_messages
                            WHERE processed_at IS NULL AND attempt_count < @MaxAttempts
                            ORDER BY occurred_at
                            LIMIT @BatchSize
                            FOR UPDATE
                            """;

        var command = new CommandDefinition(
            commandText: sql,
            parameters: new {_outboxSettings.CurrentValue.MaxAttempts, _outboxSettings.CurrentValue.BatchSize},
            transaction: transaction,
            cancellationToken: cancellationToken
        );

        var outboxMessages = await connection.QueryAsync<OutboxMessage>(command);
        return outboxMessages.ToList();
    }

    private static async ValueTask<int> MarkMessagesAsProcessed(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyCollection<OutboxUpdate> updates,
        CancellationToken cancellationToken = default)
    {
        // Serialize all updates to JSON array once
        var updatesJson = JsonSerializer.Serialize(updates, SnakeCaseNamingJsonSerializerOptions);

        // Create a single UPDATE statement using `json_to_recordset` for efficient bulk update

        const string sql = $"""
                            WITH
                                data AS (
                                    SELECT
                                        id,
                                        processed_at,
                                        error
                                    FROM
                                        json_to_recordset(@updatesJson::json) AS x (id UUID, processed_at timestamptz, error text)
                                )
                            UPDATE outbox_messages AS m
                            SET
                                processed_at = data.processed_at,
                                error = data.error,
                                attempt_count = CASE
                                    WHEN data.error IS NULL THEN m.attempt_count
                                    ELSE m.attempt_count + 1
                                END
                            FROM data
                            WHERE m.id = data.id;
                            """;
        var command = new CommandDefinition(
            commandText: sql, 
            parameters: new { updatesJson },
            transaction: transaction, 
            cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }

    private static async ValueTask PublishMessage(OutboxMessage message,
        ConcurrentQueue<OutboxUpdate> updateQueue,
        IPublisher publisher,
        IClock clock,
        IOutboxMessageResolver outboxMessageResolver,
        ILogger<OutboxProcessor> logger,
        CancellationToken cancellationToken = default
    )
    {
        var result = outboxMessageResolver.DeserializeEvent(type: message.Type, content: message.Content);
        if (result.IsFailure)
        {
            var errorText = result.Error.Message;
            OutboxMessagesProcessorLoggers.LogFailedToDeserialize(logger, new Exception(errorText),message.Id);
            updateQueue.Enqueue(
                new OutboxUpdate(Id: message.Id,
                    ProcessedOnUtc: clock.UtcNow,
                    Error: errorText));
            return;
        }
        
        try
        {
            var deserialized = result.Value;
            await publisher.Publish(deserialized, cancellationToken);
            updateQueue.Enqueue(
                new OutboxUpdate(Id: message.Id,
                    ProcessedOnUtc: clock.UtcNow,
                    Error: null));
        }
        catch (Exception ex)
        {
            OutboxMessagesProcessorLoggers.LogFailedToPublish(logger, ex, message.Id);
            
            updateQueue.Enqueue(
                new OutboxUpdate(Id: message.Id,
                    ProcessedOnUtc: null,
                    Error: ex.ToString()));
        }
    }
        

    private readonly record struct OutboxUpdate(
        Guid Id,
        DateTimeOffset? ProcessedOnUtc,
        string? Error
    );
}


internal static partial class OutboxMessagesProcessorLoggers
{
    [LoggerMessage(Level = LogLevel.Error,
        Message = "Failed to publish outbox message {OutboxMessageId}")]
    internal static partial void LogFailedToPublish(ILogger logger, Exception exception, Guid outboxMessageId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Failed to deserialize outbox message {OutboxMessageId}")]
    internal static partial void LogFailedToDeserialize(ILogger logger, Exception exception, Guid outboxMessageId);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "An error occurred in OutboxProcessor")]
    internal static partial void LogError(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "OutboxProcessor cancelled")]
    internal static partial void LogCancelled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug,
        Message =
            "Outbox processing completed. Total time: {TotalTime}ms, Query time: {QueryTime}ms," +
            " Publish time: {PublishTime}ms, Update time: {UpdateTime}ms, Messages processed: {MessageCount}," +
            " Rows affected: {RowsAffected}")]
    internal static partial void LogProcessingPerformance(ILogger logger,
        long totalTime,
        long queryTime,
        long publishTime,
        long updateTime,
        int messageCount,
        int rowsAffected);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "No outbox messages to process.")]
    internal static partial void LogNoMessagesToProcess(ILogger logger);
}