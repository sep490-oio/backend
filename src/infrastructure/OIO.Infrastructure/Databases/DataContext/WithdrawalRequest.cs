using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class WithdrawalRequest
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid WalletId { get; set; }

    public decimal Amount { get; set; }

    public decimal? Fee { get; set; }

    public decimal NetAmount { get; set; }

    public string? BankName { get; set; }

    public string? BankAccountNumber { get; set; }

    public string? BankAccountHolder { get; set; }

    public string Status { get; set; } = null!;

    public Guid? ProcessedBy { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? ProcessedByNavigation { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Wallet Wallet { get; set; } = null!;
}
