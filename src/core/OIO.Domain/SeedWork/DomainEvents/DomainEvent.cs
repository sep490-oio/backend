namespace OIO.Domain.SeedWork.DomainEvents;

public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent(DateTime occurredAt)
        : this(Guid.CreateVersion7(), occurredAt)
    {
    }

    protected DomainEvent(Guid id, DateTime occurredAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Event id must not be empty.", nameof(id));
        }

        ValidateUtcTime(occurredAt);

        EventId = id;
        OccurredAt = occurredAt;
    }

    public Guid EventId { get; init; }

    public DateTime OccurredAt { get; init; }

    public virtual string EventType => GetType().Name;

    private static void ValidateUtcTime(DateTime dateTime)
    {
        if (dateTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must be in UTC.", nameof(dateTime));
        }
    }
}