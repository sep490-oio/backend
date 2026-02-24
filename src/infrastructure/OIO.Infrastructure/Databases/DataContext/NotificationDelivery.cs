using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class NotificationDelivery
{
    public Guid Id { get; set; }

    public Guid NotificationId { get; set; }

    public Guid UserId { get; set; }

    public string Channel { get; set; } = null!;

    public string? Status { get; set; }

    public int? AttemptCount { get; set; }

    public int? MaxAttempts { get; set; }

    public DateTime? NextRetryAt { get; set; }

    public string? DeliveryMetadata { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ErrorDetails { get; set; }

    public DateTime ScheduledAt { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? FailedAt { get; set; }

    public virtual Notification Notification { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
