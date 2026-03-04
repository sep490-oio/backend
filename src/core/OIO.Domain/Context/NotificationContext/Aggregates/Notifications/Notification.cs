using CSharpFunctionalExtensions;
using OIO.Domain.Context.NotificationContext.Aggregates.Notifications.Events;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.NotificationContext.Errors;
using OIO.Domain.Context.NotificationContext.ValueObjects;
using OIO.Domain.Context.NotificationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using e = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Domain.Context.NotificationContext.Aggregates.Notifications;

public sealed class Notification : AggregateRoot<NotificationId>, ICreatedAtEntity
{
    private readonly List<NotificationDelivery> _deliveries = [];

#pragma warning disable CS8618
    private Notification() { }
#pragma warning restore CS8618

    public UserId UserId { get; private set; }

    public string NotificationType { get; private set; }

    public string EventType { get; private set; }

    public string Title { get; private set; }

    public string Message { get; private set; }

    public NotificationMetadata Metadata { get; private set; }

    /// <summary>Optional entity this notification is primarily about (e.g. an order).</summary>
    public string? EntityType { get; private set; }

    public Guid? EntityId { get; private set; }

    public RelatedEntities RelatedEntities { get; private set; }

    public NotificationStatus Status { get; private set; }

    public NotificationPriority Priority { get; private set; }

    public NotificationActions Actions { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ModifiedAt { get; private set; }

    public DateTime? ReadAt { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public IReadOnlyList<NotificationDelivery> Deliveries => _deliveries;

    public bool IsExpired(DateTime now) => ExpiresAt.HasValue && now >= ExpiresAt.Value;

    private Notification(
        NotificationId id,
        UserId userId,
        string notificationType,
        string eventType,
        string title,
        string message,
        NotificationPriority priority,
        NotificationMetadata metadata,
        RelatedEntities relatedEntities,
        NotificationActions actions,
        DateTime createdAt,
        string? entityType = null,
        Guid? entityId = null,
        DateTime? expiresAt = null)
    {
        Id = id;
        UserId = userId;
        NotificationType = notificationType;
        EventType = eventType;
        Title = title;
        Message = message;
        Priority = priority;
        Metadata = metadata;
        RelatedEntities = relatedEntities;
        Actions = actions;
        EntityType = entityType;
        EntityId = entityId;
        Status = NotificationStatus.Unread;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    public static Notification Create(
        UserId userId,
        string notificationType,
        string eventType,
        string title,
        string message,
        NotificationPriority priority,
        DateTime now,
        NotificationMetadata? metadata = null,
        RelatedEntities? relatedEntities = null,
        NotificationActions? actions = null,
        string? entityType = null,
        Guid? entityId = null,
        DateTime? expiresAt = null)
    {
        var notification = new Notification(
            id: NotificationId.From(Guid.CreateVersion7()),
            userId: userId,
            notificationType: notificationType,
            eventType: eventType,
            title: title,
            message: message,
            priority: priority,
            metadata: metadata ?? NotificationMetadata.Empty,
            relatedEntities: relatedEntities ?? RelatedEntities.Empty,
            actions: actions ?? NotificationActions.Empty,
            createdAt: now,
            entityType: entityType,
            entityId: entityId,
            expiresAt: expiresAt);

        notification.RaiseDomainEvent(new NotificationCreatedEvent(
            notification.Id.ToString(),
            userId.ToString(),
            notificationType,
            eventType,
            priority.Id,
            now));

        return notification;
    }

    public UnitResult<e> MarkAsRead(DateTime now)
    {
        if (Status == NotificationStatus.Read)
            return UnitResult.Success<e>();

        if (Status == NotificationStatus.Deleted)
            return NotificationErrors.Notification.NotificationDeleted;

        Status = NotificationStatus.Read;
        ReadAt = now;
        ModifiedAt = now;

        RaiseDomainEvent(new NotificationReadEvent(Id.ToString(), UserId.ToString(), now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> Archive(DateTime now)
    {
        if (Status == NotificationStatus.Archived)
            return UnitResult.Success<e>();

        if (Status == NotificationStatus.Deleted)
            return NotificationErrors.Notification.NotificationDeleted;

        Status = NotificationStatus.Archived;
        ModifiedAt = now;

        RaiseDomainEvent(new NotificationArchivedEvent(Id.ToString(), UserId.ToString(), now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> Delete(DateTime now)
    {
        if (Status == NotificationStatus.Deleted)
            return UnitResult.Success<e>();

        Status = NotificationStatus.Deleted;
        ModifiedAt = now;

        foreach (var delivery in _deliveries.Where(d => d.Status == DeliveryStatus.Pending))
            delivery.Cancel();

        RaiseDomainEvent(new NotificationDeletedEvent(Id.ToString(), UserId.ToString(), now));

        return UnitResult.Success<e>();
    }

    // ==================== Delivery Management ====================

    public Result<NotificationDelivery, Error> AddDelivery(
        DeliveryChannel channel,
        DateTime scheduledAt,
        int maxAttempts = 3)
    {
        if (Status == NotificationStatus.Deleted)
            return NotificationErrors.Notification.NotificationDeleted;

        var delivery = new NotificationDelivery(Id, UserId, channel, scheduledAt, maxAttempts);
        _deliveries.Add(delivery);

        return delivery;
    }

    public UnitResult<e> RecordDeliverySent(
        NotificationDeliveryId deliveryId,
        DeliveryMetadata metadata,
        DateTime now)
    {
        var delivery = _deliveries.FirstOrDefault(d => d.Id == deliveryId);
        if (delivery is null)
            return NotificationErrors.Delivery.DeliveryNotFound(deliveryId);

        var result = delivery.RecordSent(metadata, now);
        if (result.IsFailure)
            return result.Error;

        RaiseDomainEvent(new NotificationDeliveryAttemptedEvent(
            Id.ToString(), deliveryId.ToString(), UserId.ToString(),
            delivery.Channel.Id, true, delivery.AttemptCount, now));

        return UnitResult.Success<e>();
    }

    public UnitResult<e> RecordDeliveryConfirmed(
        NotificationDeliveryId deliveryId,
        DateTime now)
    {
        var delivery = _deliveries.FirstOrDefault(d => d.Id == deliveryId);
        if (delivery is null)
            return NotificationErrors.Delivery.DeliveryNotFound(deliveryId);

        return delivery.RecordDelivered(now);
    }

    public UnitResult<e> RecordDeliveryFailed(
        NotificationDeliveryId deliveryId,
        DeliveryError error,
        DateTime now,
        DateTime? nextRetryAt = null)
    {
        var delivery = _deliveries.FirstOrDefault(d => d.Id == deliveryId);
        if (delivery is null)
            return NotificationErrors.Delivery.DeliveryNotFound(deliveryId);

        var result = delivery.RecordFailed(error, now, nextRetryAt);
        if (result.IsFailure)
            return result.Error;

        var willRetry = delivery.CanRetry;

        RaiseDomainEvent(new NotificationDeliveryFailedEvent(
            Id.ToString(), deliveryId.ToString(), UserId.ToString(),
            delivery.Channel.Id, error.Code, delivery.AttemptCount, willRetry, now));

        if (delivery.IsExhausted)
        {
            RaiseDomainEvent(new NotificationDeliveryExhaustedEvent(
                Id.ToString(), deliveryId.ToString(), UserId.ToString(),
                delivery.Channel.Id, error.Code, now));
        }

        return UnitResult.Success<e>();
    }
}