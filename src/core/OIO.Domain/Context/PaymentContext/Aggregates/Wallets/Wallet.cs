using CSharpFunctionalExtensions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.PaymentContext.ValueObjects;
using OIO.Domain.Context.PaymentContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.DomainEvents;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.PaymentContext.Aggregates.Wallets;

public sealed class Wallet : AggregateRoot<WalletId>, IAuditableEntity, IVersionEntity
{
    private readonly List<WalletTransaction> _walletTransactions = [];

    public UserId? UserId { get; private set; }
    public WalletType Type { get; private set; }
    public WalletFunds WalletFunds {get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ModifiedAt { get; private set; }

    // Navigation
    public User User { get; private set; }
    public IReadOnlyCollection<WalletTransaction> WalletTransactions => _walletTransactions.AsReadOnly();

    private Wallet() { }

    public UnitResult<Error> Activate(DateTime nowUtc)
    {
        if (IsActive)
            return UnitResult.Success<Error>();

        IsActive = true;
        ModifiedAt = nowUtc;
        return UnitResult.Success<Error>();
    }

    private UnitResult<Error> EnsureActive()
    {
        if (!IsActive)
            return Error.Conflict("Wallet.NotActive", "Wallet is not active. Please deposit funds to activate.");

        return UnitResult.Success<Error>();
    }

    public static Wallet Create(
        UserId userId,
        Currency currency,
        DateTime nowUtc)
    {
        return new Wallet()
        {
            Id = WalletId.From(Guid.CreateVersion7()),
            UserId = userId,
            Type = WalletType.Personal,
            WalletFunds = WalletFunds.Empty(currency),
            IsActive = false,
            Version = 1,
            CreatedAt = nowUtc,
        };
    }

    public static Wallet CreatePlatformWallet(Currency currency, DateTime nowUtc)
    {
        return new Wallet
        {
            Id = WalletId.From(Guid.CreateVersion7()),
            UserId = null,
            Type = WalletType.Platform,
            WalletFunds = WalletFunds.Empty(currency),
            IsActive = true,
            Version = 1,
            CreatedAt = nowUtc,
        };
    }

    /// <summary>
    /// Nạp tiền vào ví (credit). Tự động tạo WalletTransaction ghi nhận.
    /// </summary>
    public UnitResult<Error> Credit(
        decimal amount,
        TransactionId? transactionId,
        string? description,
        DateTime nowUtc)
    {
        if (!IsActive)
            Activate(nowUtc);

        var balanceBefore = WalletFunds.BalanceAmount;

        var creditResult = WalletFunds.Credit(amount);
        if (creditResult.IsFailure)
            return creditResult.Error;

        WalletFunds = creditResult.Value;

        _walletTransactions.Add(WalletTransaction.Create(
            walletId: Id,
            type: WalletTransactionType.Credit,
            amount: amount,
            balanceBefore: balanceBefore,
            balanceAfter: WalletFunds.BalanceAmount,
            transactionId: transactionId,
            description: description,
            nowUtc: nowUtc));

        ModifiedAt = nowUtc;

        if (UserId is { } userId)
            RaiseDomainEvent(new WalletCreditedDomainEvent(
                Id, userId, amount, WalletFunds.BalanceAmount, description, nowUtc));

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Trừ tiền từ ví (debit). Tự động tạo WalletTransaction ghi nhận.
    /// </summary>
    public UnitResult<Error> Debit(
        decimal amount,
        TransactionId? transactionId,
        string? description,
        DateTime nowUtc)
    {
        var activeCheck = EnsureActive();
        if (activeCheck.IsFailure)
            return activeCheck.Error;

        var balanceBefore = WalletFunds.BalanceAmount;

        var debitResult = WalletFunds.Debit(amount);
        if (debitResult.IsFailure)
            return debitResult.Error;

        WalletFunds = debitResult.Value;

        _walletTransactions.Add(WalletTransaction.Create(
            walletId: Id,
            type: WalletTransactionType.Debit,
            amount: amount,
            balanceBefore: balanceBefore,
            balanceAfter: WalletFunds.BalanceAmount,
            transactionId: transactionId,
            description: description,
            nowUtc: nowUtc));

        ModifiedAt = nowUtc;

        if (UserId is { } userId)
            RaiseDomainEvent(new WalletDebitedDomainEvent(
                Id, userId, amount, WalletFunds.BalanceAmount, description, nowUtc));

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Giữ tiền (Hold): Chuyển từ số dư khả dụng sang số dư đang chờ. Tạo WalletTransaction.
    /// </summary>
    public UnitResult<Error> Hold(
        decimal amount,
        TransactionId? transactionId,
        string? description,
        DateTime nowUtc)
    {
        var activeCheck = EnsureActive();
        if (activeCheck.IsFailure)
            return activeCheck.Error;

        var balanceBefore = WalletFunds.BalanceAmount;

        var holdResult = WalletFunds.AddPending(amount);
        if (holdResult.IsFailure)
            return holdResult.Error;

        WalletFunds = holdResult.Value;

        _walletTransactions.Add(WalletTransaction.Create(
            walletId: Id,
            type: WalletTransactionType.Hold,
            amount: amount,
            balanceBefore: balanceBefore,
            balanceAfter: WalletFunds.BalanceAmount, // Lưu ý: BalanceAmount đã bị giảm sau khi Hold
            transactionId: transactionId,
            description: description,
            nowUtc: nowUtc));

        ModifiedAt = nowUtc;

        if (UserId is { } userId)
            RaiseDomainEvent(new WalletHeldDomainEvent(
                Id, userId, amount, description, nowUtc));

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Nhả tiền giữ (Unhold): Chuyển từ số dư đang chờ về khả dụng. Tạo WalletTransaction.
    /// </summary>
    public UnitResult<Error> Unhold(
        decimal amount,
        TransactionId? transactionId,
        string? description,
        DateTime nowUtc)
    {
        var activeCheck = EnsureActive();
        if (activeCheck.IsFailure)
            return activeCheck.Error;

        var balanceBefore = WalletFunds.BalanceAmount;

        var unholdResult = WalletFunds.ReleasePending(amount);
        if (unholdResult.IsFailure)
            return unholdResult.Error;

        WalletFunds = unholdResult.Value;

        _walletTransactions.Add(WalletTransaction.Create(
            walletId: Id,
            type: WalletTransactionType.Release,
            amount: amount,
            balanceBefore: balanceBefore,
            balanceAfter: WalletFunds.BalanceAmount, // BalanceAmount sẽ tăng lên sau khi nhả hold
            transactionId: transactionId,
            description: description,
            nowUtc: nowUtc));

        ModifiedAt = nowUtc;

        if (UserId is { } userId)
            RaiseDomainEvent(new WalletUnheldDomainEvent(
                Id, userId, amount, description, nowUtc));

        return UnitResult.Success<Error>();
    }

    /// <summary>
    /// Trừ tiền đã giữ (Debit Pending): Trừ dứt điểm từ Pending. Tạo WalletTransaction.
    /// </summary>
    public UnitResult<Error> DebitPending(
        decimal amount,
        TransactionId? transactionId,
        string? description,
        DateTime nowUtc)
    {
        var activeCheck = EnsureActive();
        if (activeCheck.IsFailure)
            return activeCheck.Error;

        var balanceBefore = WalletFunds.BalanceAmount;

        var debitPendingResult = WalletFunds.DebitPending(amount);
        if (debitPendingResult.IsFailure)
            return debitPendingResult.Error;

        WalletFunds = debitPendingResult.Value;

        _walletTransactions.Add(WalletTransaction.Create(
            walletId: Id,
            type: WalletTransactionType.Debit, // Vẫn là Debit do tiền đi ra khỏi hệ thống
            amount: amount,
            balanceBefore: balanceBefore,
            balanceAfter: WalletFunds.BalanceAmount, // BalanceAmount không đổi trong DebitPending
            transactionId: transactionId,
            description: description,
            nowUtc: nowUtc));

        ModifiedAt = nowUtc;
        return UnitResult.Success<Error>();
    }
}