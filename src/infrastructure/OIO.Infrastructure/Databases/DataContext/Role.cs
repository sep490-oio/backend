using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class Role
{
    public Guid Id { get; set; }

    public string RoleName { get; set; } = null!;

    public string? NormalizedRoleName { get; set; }

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
