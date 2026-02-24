using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class DisputeStatusHistory
{
    public Guid Id { get; set; }

    public Guid DisputeId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public Guid? ChangedBy { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? ChangedByNavigation { get; set; }

    public virtual Dispute Dispute { get; set; } = null!;
}
