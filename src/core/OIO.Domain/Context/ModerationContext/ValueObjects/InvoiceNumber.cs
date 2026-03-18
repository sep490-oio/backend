using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.ModerationContext.ValueObjects;

public sealed class InvoiceNumber : ValueObject
{
    public string Value { get; }
    
    private InvoiceNumber() {}
    private InvoiceNumber(string value) => Value = value;

    public static Result<InvoiceNumber, Error> Create(string value)
    {
        var check = InvoiceNumber.Check()
            .Field(value)
            .NotWhiteSpace()
            .ToUnitResult();

        if (check.IsFailure)
            return check.Error;
        
        return new InvoiceNumber(value.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}