using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class WalletTransaction
{
    public Guid Id { get; set; }

    public Guid WalletId { get; set; }

    public Guid? TransactionId { get; set; }

    public string Type { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal BalanceBefore { get; set; }

    public decimal BalanceAfter { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Transaction? Transaction { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;
}
