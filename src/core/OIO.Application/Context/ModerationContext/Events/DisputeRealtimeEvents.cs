using MediatR;
using OIO.Domain.Context.ModerationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.ModerationContext.Events;

// These events are *in-process realtime notifications* (SignalR / read-state
// fan-out) — NOT domain events and NOT outbox events. They are published with
// publisher.Publish(...) directly after SaveChanges, with no parent row in
// outbox_messages. Inheriting from DomainEvent would cause the outbox
// IdempotentDomainEventHandler<T> decorator to wrap them and try to insert
// into outbox_message_consumers, which then fails the FK to outbox_messages.
// Keep them as plain INotification so the decorator never picks them up.

public sealed record DisputeMessageSentEvent(
    DisputeId DisputeId,
    DisputeMessageId MessageId,
    UserId SenderId,
    bool IsInternal,
    DateTime OccurredAt) : INotification;

public sealed record DisputeReadStateUpdatedEvent(
    DisputeId DisputeId,
    UserId UserId,
    DisputeMessageId LastReadMessageId,
    DateTime ReadAt) : INotification;

public sealed record DisputeChangedEvent(
    DisputeId DisputeId,
    DateTime OccurredAt) : INotification;
