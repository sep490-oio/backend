using CSharpFunctionalExtensions;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.PaymentContext.ValueObjects;

public sealed class WalletFunds : ValueObject
{
    public decimal BalanceAmount { get; private init; }
    public decimal PendingBalanceAmount { get; private init; }
    public Currency Currency { get; private init; }

    public Money Balance => Money.Of(BalanceAmount, Currency);

    public Money PendingBalance => Money.Of(PendingBalanceAmount, Currency);

    public static WalletFunds Empty(Currency currency) => new(0m, 0m, currency);

    private WalletFunds() { } // EF

    private WalletFunds(
        decimal balance,
        decimal pendingBalance,
        Currency currency)
    {
        Currency = currency;
        BalanceAmount = balance;
        PendingBalanceAmount = pendingBalance;
    }

    public static Result<WalletFunds, Error> Create(
        decimal balance,
        decimal pendingBalance,
        Currency currency)
    {
        var check = WalletFunds.Check()
            .Field(balance, x => x.Balance)
            .NonNegative()
            .Field(pendingBalance, x  => x.PendingBalance)
            .NonNegative()
            .ToUnitResult();
        
        if (check.IsFailure)
            return check.Error;

        return new WalletFunds(balance, pendingBalance, currency);
    }
    


    public Result<WalletFunds, Error> Credit(decimal amount)
    {
        var check = WalletFunds.Check()
            .Field(amount, x => x.Balance)
            .NonNegative()
            .ToUnitResult();

        if (check.IsFailure)
            return check.Error;
        
        return new WalletFunds(BalanceAmount + amount, PendingBalanceAmount, Currency);
    }

    /// <summary>
    /// Trừ tiền trực tiếp từ số dư khả dụng (Available Balance).
    /// </summary>
    public Result<WalletFunds, Error> Debit(decimal amount)
    {
        var check = WalletFunds.Check()
            .Field(amount, x => x.Balance)
            .NonNegative()
            .LessThanOrEqual(BalanceAmount) // Phải nhỏ hơn hoặc bằng số dư khả dụng
            .ToUnitResult();

        if (check.IsFailure)
            return check.Error;

        return new WalletFunds(BalanceAmount - amount, PendingBalanceAmount, Currency);
    }

    /// <summary>
    /// Giữ tiền (Hold): Chuyển từ số dư khả dụng (Available) sang số dư đang chờ (Pending).
    /// </summary>
    public Result<WalletFunds, Error> AddPending(decimal amount)
    {
        var check = WalletFunds.Check()
            .Field(amount, x => x.Balance)
            .NonNegative()
            .LessThanOrEqual(BalanceAmount) // Hold tiền thì ví khả dụng phải đủ tiền
            .ToUnitResult();

        if (check.IsFailure)
            return check.Error;
        
        return new WalletFunds(BalanceAmount - amount, PendingBalanceAmount + amount, Currency);
    }

    /// <summary>
    /// Hoàn tiền đang giữ (Unhold): Chuyển từ Pending trở lại Available Balance.
    /// </summary>
    public Result<WalletFunds, Error> ReleasePending(decimal amount)
    {
        var check = WalletFunds.Check()
            .Field(amount, x => x.Balance)
            .NonNegative()
            .LessThanOrEqual(PendingBalanceAmount) // Chỉ được nhả hold số tiền đang hold
            .ToUnitResult();
        
        if(check.IsFailure)
            return check.Error;
        
        return new WalletFunds(BalanceAmount + amount, PendingBalanceAmount - amount, Currency);
    }

    /// <summary>
    /// Trừ tiền đã giữ (Debit Pending): Trừ dứt điểm từ Pending (không hoàn lại ví).
    /// Dùng khi giao dịch hoàn tất và cần cắt tiền.
    /// </summary>
    public Result<WalletFunds, Error> DebitPending(decimal amount)
    {
        var check = WalletFunds.Check()
            .Field(amount, x => x.Balance)
            .NonNegative()
            .LessThanOrEqual(PendingBalanceAmount) // Khoản trừ phải nhỏ hơn hoặc bằng số tiền đang hold
            .ToUnitResult();

        if (check.IsFailure)
            return check.Error;

        // Trừ thẳng vào PendingBalance, AvailableBalance không bị thay đổi.
        return new WalletFunds(BalanceAmount, PendingBalanceAmount - amount, Currency);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return BalanceAmount;
        yield return PendingBalanceAmount;
        yield return Currency.Id;
    }
}