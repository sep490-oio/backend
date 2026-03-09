using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.Shared.ValueObjects;

public class Money : ValueObject, IComparable<Money>
{
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }
    
    private Money() {}
    
    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }
    
    public static Result<Money, Error> Create(decimal amount, string currency)
    {
        var result = Money.Check(isInvariant: true)
            .Field(amount)
            .NonNegative()
            .Field(currency)
            .InSet(Currency.All.Select(g => g.Id))
            .ToUnitResult();

        if (result.IsFailure)
        {
            return result.Error;
        }
        
        return new Money(Math.Round(amount, 2), Currency.FromId(currency).GetValueOrThrow());
    }
    
    public static Result<Money, Error> Create(decimal amount, Currency currency)
    {
        var result = Money.Check(isInvariant: true)
            .Field(amount)
            .NonNegative()
            .Field(currency)
            .ToUnitResult();

        if (result.IsFailure)
        {
            return result.Error;
        }
        
        return new Money(Math.Round(amount, 2), currency);
    }

    public static Money Zero(Currency currency) => new(0, currency);

    public bool IsZero() => this == Zero(Currency);
    
    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return Amount - other.Amount < 0 ? throw new InvalidOperationException("Result cannot be negative.") : new Money(Amount - other.Amount, Currency);
    }

    public bool IsGreaterThan(Money other)
    {
        EnsureSameCurrency(other);
        return Amount > other.Amount;
    }

    public bool IsGreaterThanOrEqual(Money other)
    {
        EnsureSameCurrency(other);
        return Amount >= other.Amount;
    }

    public int CompareTo(Money? other)
    {
        if (other is null) return 1;
        EnsureSameCurrency(other);
        return Amount.CompareTo(other.Amount);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (!Currency.Equals(other.Currency))
            throw new InvalidOperationException(
                $"Cannot operate on different currencies: {Currency} vs {other.Currency}");
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
    
    public override string ToString() => $"{Amount:N2}{Currency.Symbol}";

    public static bool operator >(Money left, Money right) => left.IsGreaterThan(right);
    public static bool operator <(Money left, Money right) => right.IsGreaterThan(left);
    public static bool operator >=(Money left, Money right) => left.IsGreaterThanOrEqual(right);
    public static bool operator <=(Money left, Money right) => right.IsGreaterThanOrEqual(left);
    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
}