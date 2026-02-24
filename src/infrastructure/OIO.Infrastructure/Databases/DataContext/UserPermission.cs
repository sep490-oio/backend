using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class UserPermission
{
    public Guid UserId { get; set; }

    public Guid PermissionId { get; set; }

    public bool IsAllowed { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
