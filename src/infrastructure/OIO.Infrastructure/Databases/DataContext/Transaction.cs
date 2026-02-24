using System;
using System.Collections.Generic;

namespace OIO.Infrastructure.Databases.DataContext;

public partial class Transaction
{
    public Guid Id { get; set; }

    public string TransactionNumber { get; set; } = null!;

    public Guid? OrderId { get; set; }

    public Guid UserId { get; set; }

    public Guid? PaymentMethodId { get; set; }

    public string Type { get; set; } = null!;

    public decimal Amount { get; set; }

    public decimal? Fee { get; set; }

    public decimal NetAmount { get; set; }

    public string? Currency { get; set; }

    public string Status { get; set; } = null!;

    public string? GatewayProvider { get; set; }

    public string? GatewayTransactionId { get; set; }

    public string? GatewayResponse { get; set; }

    public string? Description { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AuctionDeposit> AuctionDeposits { get; set; } = new List<AuctionDeposit>();

    public virtual DisputeRefund? DisputeRefund { get; set; }

    public virtual ICollection<Escrow> EscrowHoldTransactions { get; set; } = new List<Escrow>();

    public virtual ICollection<Escrow> EscrowReleaseTransactions { get; set; } = new List<Escrow>();

    public virtual Order? Order { get; set; }

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
