using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class Notification
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string NotificationType { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? Metadata { get; set; }

    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    public string? RelatedEntities { get; set; }

    public string Status { get; set; } = null!;

    public string Priority { get; set; } = null!;

    public string? Actions { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public virtual ICollection<NotificationDelivery> NotificationDeliveries { get; set; } = new List<NotificationDelivery>();

    public virtual User User { get; set; } = null!;
}
