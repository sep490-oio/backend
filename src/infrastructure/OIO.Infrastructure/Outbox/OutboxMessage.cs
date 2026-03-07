namespace OIO.Infrastructure.Outbox;

internal sealed class OutboxMessage
{
    public required Guid Id { get; init; }
    
    /// <summary>
    /// The type of the outbox message.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The serialized content of the outbox message in JSON format.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// The UTC date and time when the outbox message was created.
    /// </summary>
    public required DateTime OccurredAt { get; init; }

    /// <summary>
    /// The UTC date and time when the outbox message was processed.
    /// If null, the message has not been processed yet.
    /// </summary>
    public DateTime? ProcessedAt { get; init; }

    /// <summary>
    /// The error message if processing the outbox message failed.
    /// If null, the message was processed successfully.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// The number of attempts made to process the outbox message.
    /// </summary>
    public int AttemptCount { get; init; }
}

public sealed class OutboxMessageConsumer(Guid outboxMessageId, string name)
{
    public Guid OutboxMessageId { get; init; } = outboxMessageId;

    public string Name { get; init; } = name;
}