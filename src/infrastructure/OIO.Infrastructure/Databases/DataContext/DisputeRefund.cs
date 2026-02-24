using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class DisputeRefund
{
    public Guid Id { get; set; }

    public Guid DisputeId { get; set; }

    public Guid TransactionId { get; set; }

    public string RefundType { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? ApprovedByNavigation { get; set; }

    public virtual Dispute Dispute { get; set; } = null!;

    public virtual Transaction Transaction { get; set; } = null!;
}
