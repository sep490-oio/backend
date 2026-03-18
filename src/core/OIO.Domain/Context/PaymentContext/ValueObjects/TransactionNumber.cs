using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.PaymentContext.ValueObjects;

public sealed class TransactionNumber : ValueObject
{
    public string Value { get; }

    public TransactionNumber()
    {
        
    }
    
    private TransactionNumber(string value) => Value = value;

    public static Result<TransactionNumber, Error> Create(string value)
    {
        var check = TransactionNumber.Check(isInvariant: true)
            .Field(value)
            .NotWhiteSpace()
            .ToUnitResult();
        
        if (check.IsFailure)
            return check.Error;
        
        return new TransactionNumber(value.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}