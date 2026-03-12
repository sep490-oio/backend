using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.ModerationContext.ValueObjects;

public sealed class DisputeNumber : ValueObject
{
    public string Value { get; }
    
    private DisputeNumber() {}
    private DisputeNumber(string value) => Value = value;

    public static Result<DisputeNumber, Error> Create(string value)
    {
        var check = DisputeNumber.Check()
            .Field(value)
            .NotWhiteSpace()
            .ToUnitResult();

        if (check.IsFailure)
            return check.Error;
        
        return new DisputeNumber(value.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}