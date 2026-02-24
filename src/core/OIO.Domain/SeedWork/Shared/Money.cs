namespace OIO.Domain.SeedWork.Shared;

public record Money(decimal Amount, Currency Currency)
{
    public static Money operator +(Money first, Money second)
    {
        return first.Currency != second.Currency ? throw new InvalidOperationException("Currencies have to be equal") : new Money(first.Amount + second.Amount, first.Currency);
    }

    public static Money Zero(Currency currency) => new(0, currency);

    public bool IsZero() => this == Zero(Currency);
}