using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class UserNotificationPreference
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public bool? IsEnabled { get; set; }

    public string? TypePreferences { get; set; }

    public string? Channels { get; set; }

    public string? QuietHours { get; set; }

    public string? RateLimits { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
