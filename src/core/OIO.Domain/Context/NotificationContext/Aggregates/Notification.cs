using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.NotificationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.NotificationContext.Aggregates;

public sealed class Notification : AggregateRoot<NotificationId>, IAuditableEntity
{
    private readonly List<NotificationDelivery> _deliveries = [];

    public UserId UserId { get; private set; }
    public string NotificationType { get; private set; }
    public string EventType { get; private set; }
    public string Title { get; private set; }
    public string Message { get; private set; }
    public string? Metadata { get; private set; }           // jsonb
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? RelatedEntities { get; private set; }    // jsonb array
    public NotificationStatus Status { get; private set; }
    public NotificationPriority Priority { get; private set; }
    public string? Actions { get; private set; }            // jsonb array
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    // Navigation
    public IReadOnlyCollection<NotificationDelivery> Deliveries => _deliveries.AsReadOnly();

    private Notification() { }

    public static Notification Create(
        Guid userId,
        string notificationType,
        string eventType,
        string title,
        string message,
        NotificationPriority? priority = null,
        string? entityType = null,
        Guid? entityId = null,
        string? metadata = null,
        string? relatedEntities = null,
        string? actions = null,
        DateTime? expiresAt = null)
    {
        return new Notification
        {
            Id = NotificationId.From(Guid.NewGuid()),
            UserId = UserId.From(userId),
            NotificationType = notificationType,
            EventType = eventType,
            Title = title,
            Message = message,
            Priority = priority ?? NotificationPriority.Normal,
            Status = NotificationStatus.Unread,
            EntityType = entityType,
            EntityId = entityId,
            Metadata = metadata,
            RelatedEntities = relatedEntities,
            Actions = actions,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    public void MarkAsRead(DateTime readAt)
    {
        if (Status != NotificationStatus.Read)
        {
            Status = NotificationStatus.Read;
            ReadAt = readAt;
            ModifiedAt = readAt;
        }
    }
}