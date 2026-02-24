using System;
using System.Collections.Generic;
using System.Net;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class UserRefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public Guid FamilyId { get; set; }

    public Guid? ParentTokenId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? RevokedReason { get; set; }

    public IPAddress CreatedByIp { get; set; } = null!;

    public IPAddress? RevokedByIp { get; set; }

    public int RotationCounter { get; set; }

    public bool IsUsed { get; set; }

    public DateTime? UsedAt { get; set; }

    public virtual UserRefreshTokenFamily Family { get; set; } = null!;

    public virtual ICollection<UserRefreshToken> InverseParentToken { get; set; } = new List<UserRefreshToken>();

    public virtual UserRefreshToken? ParentToken { get; set; }

    public virtual User User { get; set; } = null!;
}
