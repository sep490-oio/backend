using OIO.Domain.SeedWork.DomainEvents;

namespace OIO.Domain.Context.NotificationContext.Aggregates.Notifications.Events;

public sealed record NotificationCreatedEvent(
    string NotificationId,
    string UserId,
    string NotificationType,
    string EventType,
    string Priority,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record NotificationReadEvent(
    string NotificationId,
    string UserId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record NotificationArchivedEvent(
    string NotificationId,
    string UserId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record NotificationDeletedEvent(
    string NotificationId,
    string UserId,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record NotificationDeliveryAttemptedEvent(
    string NotificationId,
    string DeliveryId,
    string UserId,
    string Channel,
    bool IsSuccess,
    int AttemptCount,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record NotificationDeliveryFailedEvent(
    string NotificationId,
    string DeliveryId,
    string UserId,
    string Channel,
    string ErrorCode,
    int AttemptCount,
    bool WillRetry,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);

public sealed record NotificationDeliveryExhaustedEvent(
    string NotificationId,
    string DeliveryId,
    string UserId,
    string Channel,
    string ErrorCode,
    DateTime OccurredAt)
    : DomainEvent(OccurredAt);
