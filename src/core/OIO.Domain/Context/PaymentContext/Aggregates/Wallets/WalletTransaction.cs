using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.PaymentContext.Aggregates.Wallets;

public sealed class WalletTransaction : BaseEntity<WalletTransactionId>, ICreatedAtEntity
{
    public WalletId WalletId { get; private set; }
    public TransactionId? TransactionId { get; private set; }
    public WalletTransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BalanceBefore { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Wallet Wallet { get; private set; } = null!;
    public Transaction? Transaction {get; private set;} 

    private WalletTransaction() { }
}