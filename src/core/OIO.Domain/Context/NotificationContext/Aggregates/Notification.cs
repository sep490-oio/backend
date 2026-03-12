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
}