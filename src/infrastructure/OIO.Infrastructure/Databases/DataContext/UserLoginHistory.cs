using System;
using System.Collections.Generic;
using System.Net;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class UserLoginHistory
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public IPAddress IpAddress { get; set; } = null!;

    public string UserAgent { get; set; } = null!;

    public DateTime LoginAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
