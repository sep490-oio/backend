using System;
using System.Collections.Generic;
using System.Net;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class UserRefreshTokenFamily
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid DeviceId { get; set; }

    public string UserAgent { get; set; } = null!;

    public IPAddress IpAddress { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastRotatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? RevokedReason { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<UserRefreshToken> UserRefreshTokens { get; set; } = new List<UserRefreshToken>();
}
