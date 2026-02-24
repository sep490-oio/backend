using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class DisputeMessage
{
    public Guid Id { get; set; }

    public Guid DisputeId { get; set; }

    public Guid SenderId { get; set; }

    public string Message { get; set; } = null!;

    public bool? IsInternal { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Dispute Dispute { get; set; } = null!;

    public virtual User Sender { get; set; } = null!;
}
