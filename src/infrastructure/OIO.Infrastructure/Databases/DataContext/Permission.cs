using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class Permission
{
    public Guid Id { get; set; }

    public string PermissionCode { get; set; } = null!;

    public string? NormalizedPermissionCode { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public virtual ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
