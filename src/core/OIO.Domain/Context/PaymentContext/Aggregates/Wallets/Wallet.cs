using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.PaymentContext.Aggregates.Wallets;

public sealed class Wallet : AggregateRoot<WalletId>, IAuditableEntity, IVersionEntity
{
    private readonly List<WalletTransaction> _walletTransactions = [];

    public UserId UserId { get; private set; }
    public WalletFunds WalletFunds {get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    // Navigation
    public User User { get; private set; }
    public IReadOnlyCollection<WalletTransaction> WalletTransactions => _walletTransactions.AsReadOnly();

    private Wallet() { }

    public static Wallet Create(
        UserId userId,
        Currency currency,
        DateTime nowUtc)
    {
        return new Wallet()
        {
            Id = WalletId.From(Guid.CreateVersion7()),
            UserId = userId,
            WalletFunds = WalletFunds.Empty(currency),
            IsActive = false,
            Version = 1,
            CreatedAt = nowUtc,
        };
    }
}