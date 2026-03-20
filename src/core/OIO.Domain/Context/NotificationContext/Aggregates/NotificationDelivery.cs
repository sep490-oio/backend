using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.NotificationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.NotificationContext.Aggregates;

public sealed class NotificationDelivery : BaseEntity<NotificationDeliveryId>
{
    public NotificationId NotificationId { get; private set; }
    public UserId UserId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public NotificationDeliveryStatus Status { get; private set; }
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

    public static NotificationDelivery Create(
        NotificationId notificationId,
        Guid userId,
        NotificationChannel channel,
        DateTime scheduledAt,
        int maxAttempts = 3)
    {
        return new NotificationDelivery
        {
            Id = NotificationDeliveryId.From(Guid.NewGuid()),
            NotificationId = notificationId,
            UserId = UserId.From(userId),
            Channel = channel,
            Status = NotificationDeliveryStatus.Pending,
            AttemptCount = 0,
            MaxAttempts = maxAttempts,
            ScheduledAt = scheduledAt,
        };
    }

    public void MarkAsSent(DateTime sentAt, string? metadata = null)
    {
        Status = NotificationDeliveryStatus.Sent;
        SentAt = sentAt;
        DeliveryMetadata = metadata;
    }

    public void MarkAsFailed(string errorCode, string errorMessage, DateTime failedAt, DateTime? nextRetryAt = null)
    {
        AttemptCount++;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        
        if (AttemptCount >= MaxAttempts || nextRetryAt is null)
        {
            Status = NotificationDeliveryStatus.Failed;
            FailedAt = failedAt;
        }
        else
        {
            NextRetryAt = nextRetryAt;
        }
    }
}