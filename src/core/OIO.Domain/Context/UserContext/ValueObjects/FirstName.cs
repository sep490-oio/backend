using CSharpFunctionalExtensions;
using OIO.Domain.Constants;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public sealed class FirstName : ValueObject
{
    private FirstName(string value)
    {
        Value = value;
    }
    
    public string Value { get; private set; }

    public static Result<FirstName, Error> Create(string? value)
    {
        var validateResult = FirstName
            .Field(value, nameof(Value))
            .NotNullOrWhiteSpace()
            .MaxLength(Constraints.FirstName.MaxLength)
            .MinLength(Constraints.FirstName.MinLength)
            .ToViolationsError();

        if (validateResult.HasErrors)
        {
            return validateResult;
        }
        
        return new FirstName(value!);
    }
    
    public override string ToString() => Value;
    
    public static implicit operator string(FirstName userName) => userName.Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}