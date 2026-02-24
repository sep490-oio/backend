using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class Escrow
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid? HoldTransactionId { get; set; }

    public Guid? ReleaseTransactionId { get; set; }

    public decimal Amount { get; set; }

    public string? Currency { get; set; }

    public string Status { get; set; } = null!;

    public DateTime HeldAt { get; set; }

    public DateTime? ReleasedAt { get; set; }

    public string? ReleasedTo { get; set; }

    public virtual Transaction? HoldTransaction { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Transaction? ReleaseTransaction { get; set; }
}
