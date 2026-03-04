using CSharpFunctionalExtensions;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.NotificationContext.ValueObjects;
using OIO.Domain.Context.NotificationContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;
using e = OIO.Domain.SeedWork.Errors.Error;

namespace OIO.Domain.Context.NotificationContext.Aggregates.Notifications;

public sealed class NotificationDelivery : BaseEntity<NotificationDeliveryId>
{
    private const int DefaultMaxAttempts = 3;

#pragma warning disable CS8618
    private NotificationDelivery() { }
#pragma warning restore CS8618

    public NotificationId NotificationId { get; private set; }

    public UserId UserId { get; private set; }

    public DeliveryChannel Channel { get; private set; }

    public DeliveryStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public int MaxAttempts { get; private set; }

    public DateTime? NextRetryAt { get; private set; }

    public DeliveryMetadata Metadata { get; private set; }

    public DeliveryError? Error { get; private set; }

    public DateTime ScheduledAt { get; private set; }

    public DateTime? SentAt { get; private set; }

    public DateTime? DeliveredAt { get; private set; }

    public DateTime? FailedAt { get; private set; }

    public bool CanRetry => AttemptCount < MaxAttempts && Status == DeliveryStatus.Failed;

    public bool IsExhausted => AttemptCount >= MaxAttempts && Status == DeliveryStatus.Failed;

    internal NotificationDelivery(
        NotificationId notificationId,
        UserId userId,
        DeliveryChannel channel,
        DateTime scheduledAt,
        int maxAttempts = DefaultMaxAttempts)
    {
        Id = NotificationDeliveryId.From(Guid.CreateVersion7());
        NotificationId = notificationId;
        UserId = userId;
        Channel = channel;
        Status = DeliveryStatus.Pending;
        AttemptCount = 0;
        MaxAttempts = maxAttempts;
        Metadata = DeliveryMetadata.Empty;
        ScheduledAt = scheduledAt;
    }

    internal UnitResult<Error> RecordSent(DeliveryMetadata metadata, DateTime now)
    {
        if (Status == DeliveryStatus.Delivered)
            return UnitResult.Success<Error>();

        Status = DeliveryStatus.Sent;
        AttemptCount++;
        Metadata = metadata;
        SentAt = now;
        NextRetryAt = null;

        return UnitResult.Success<Error>();
    }

    internal UnitResult<Error> RecordDelivered(DateTime now)
    {
        if (Status == DeliveryStatus.Delivered)
            return UnitResult.Success<Error>();

        Status = DeliveryStatus.Delivered;
        DeliveredAt = now;
        NextRetryAt = null;

        return UnitResult.Success<Error>();
    }

    internal UnitResult<Error> RecordFailed(
        DeliveryError error,
        DateTime now,
        DateTime? nextRetryAt = null)
    {
        Status = DeliveryStatus.Failed;
        AttemptCount++;
        Error = error;
        FailedAt = now;
        NextRetryAt = CanRetry ? nextRetryAt : null;

        return UnitResult.Success<Error>();
    }

    internal void Cancel()
    {
        if (Status is { } s && (s == DeliveryStatus.Delivered || s == DeliveryStatus.Cancelled))
            return;

        Status = DeliveryStatus.Cancelled;
        NextRetryAt = null;
    }
}