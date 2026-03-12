using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.PaymentContext.Aggregates.Wallets;

public sealed class Wallet : AggregateRoot<WalletId>, IAuditableEntity
{
    private readonly List<WalletTransaction> _walletTransactions = [];

    public UserId UserId { get; private set; }
    public decimal Balance { get; private set; }
    public decimal PendingBalance { get; private set; }
    public string Currency { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    // Navigation
    public IReadOnlyCollection<WalletTransaction> WalletTransactions => _walletTransactions.AsReadOnly();

    private Wallet() { }
}