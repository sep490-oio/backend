using System.Data.Common;
using Dapper;
using MediatR;
using Npgsql;
using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Infrastructure.Outbox;

internal sealed class IdempotentDomainEventHandler<TDomainEvent> : INotificationHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly INotificationHandler<TDomainEvent> _decorated;

    public IdempotentDomainEventHandler(
        NpgsqlDataSource dataSource,
        INotificationHandler<TDomainEvent> decorated)
    {
        _dataSource = dataSource;
        _decorated = decorated;
    }

    public async Task Handle(TDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        var outboxMessageConsumer = new OutboxMessageConsumer(domainEvent.EventId, _decorated.GetType().Name);

        if (await OutboxConsumerExistsAsync(connection, outboxMessageConsumer))
        {
            return;
        }

        await _decorated.Handle(domainEvent, cancellationToken);

        await InsertOutboxConsumerAsync(connection, outboxMessageConsumer);
    }

    private static async Task<bool> OutboxConsumerExistsAsync(
        DbConnection dbConnection,
        OutboxMessageConsumer outboxMessageConsumer)
    {
        const string sql = $"""
                            SELECT EXISTS(
                                SELECT 1
                                FROM outbox_message_consumers
                                WHERE outbox_message_id = @OutboxMessageId AND
                                      name = @Name
                            )
                            """;

        return await dbConnection.ExecuteScalarAsync<bool>(sql, param: new { outboxMessageConsumer.OutboxMessageId, outboxMessageConsumer.Name});
    }

    private static async Task InsertOutboxConsumerAsync(
        DbConnection dbConnection,
        OutboxMessageConsumer outboxMessageConsumer)
    {
        const string sql = $"""
                            INSERT INTO outbox_message_consumers(outbox_message_id, name)
                            VALUES (@OutboxMessageId, @Name)
                            ON CONFLICT (outbox_message_id, name) DO NOTHING
                            """;

        await dbConnection.ExecuteAsync(sql, param: new { outboxMessageConsumer.OutboxMessageId, outboxMessageConsumer.Name});
    }
} 