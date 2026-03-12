using OIO.Domain.Context.NotificationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.NotificationContext.Aggregates;

public sealed class NotificationDelivery : BaseEntity<NotificationDeliveryId>
{
    public NotificationId NotificationId { get; private set; }
    public UserId UserId { get; private set; }
    public string Channel { get; private set; }
    public string? Status { get; private set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; }
    public DateTime? NextRetryAt { get; private set; }
    public string? DeliveryMetadata { get; private set; }   // jsonb
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorDetails { get; private set; }       // jsonb
    public DateTime ScheduledAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? FailedAt { get; private set; }

    // Navigation
    public Notification Notification { get; private set; } = null!;

    private NotificationDelivery() { }
}