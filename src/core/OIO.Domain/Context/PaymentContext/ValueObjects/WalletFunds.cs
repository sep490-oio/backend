using CSharpFunctionalExtensions;
using OIO.Domain.Context.Shared.Enums;
using OIO.Domain.Context.Shared.ValueObjects;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.PaymentContext.ValueObjects;

public sealed class WalletFunds
{
    private readonly decimal _balance;
    private readonly decimal _pendingBalance;
    private readonly string _currency;

    public Money Balance
    {
        get => Money.Of(_balance, _currency);
        private init => _balance = value.Amount;
    }

    public Money PendingBalance
    {
        get => Money.Of(_pendingBalance, _currency);
        private init =>  _pendingBalance = value.Amount;
    }

    public Currency Currency
    {
        get => new Currency(_currency);
        private init  => _currency = value.Id;
    } 
    public static WalletFunds Empty(Currency currency) => new(0m, 0m, currency.Id);

    private WalletFunds() { } // EF

    private WalletFunds(
        decimal balance,
        decimal pendingBalance,
        string currency)
    {
        _currency = currency;
        _balance = balance;
        _pendingBalance = pendingBalance;
    }

    public static Result<WalletFunds, Error> Create(
        decimal balance,
        decimal pendingBalance,
        string currency)
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
        
        return new WalletFunds(_balance + amount, _pendingBalance, _currency);
    }

    public Result<WalletFunds, Error> Debit(decimal amount)
    {
        var check = WalletFunds.Check()
            .Field(amount, x => x.Balance)
            .NonNegative()
            .LessThanOrEqual(_pendingBalance)
            .ToUnitResult();

        return new WalletFunds(_balance - amount, _pendingBalance, _currency);
    }

    public Result<WalletFunds, Error> AddPending(decimal amount)
    {
        var check = WalletFunds.Check()
            .Field(amount, x => x.Balance)
            .NonNegative()
            .ToUnitResult();

        if (check.IsFailure)
            return check.Error;
        
        return new WalletFunds(_balance, _pendingBalance + amount, _currency);
    }

    public Result<WalletFunds, Error> ReleasePending(decimal amount)
    {
        var check = WalletFunds.Check()
            .Field(amount, x => x.Balance)
            .NonNegative()
            .LessThanOrEqual(_pendingBalance)
            .ToUnitResult();
        
        if(check.IsFailure)
            return check.Error;
        

        return new WalletFunds(_balance, _pendingBalance - amount, _currency);
    }

}