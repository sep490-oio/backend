using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class Wallet
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public decimal Balance { get; set; }

    public decimal? PendingBalance { get; set; }

    public string? Currency { get; set; }

    public bool? IsActive { get; set; }

    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();

    public virtual ICollection<WithdrawalRequest> WithdrawalRequests { get; set; } = new List<WithdrawalRequest>();
}
