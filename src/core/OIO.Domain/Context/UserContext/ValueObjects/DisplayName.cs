using CSharpFunctionalExtensions;
using OIO.Domain.Constants;
using OIO.Domain.SeedWork.Checks;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public sealed class DisplayName : ValueObject
{
    private DisplayName(string value)
    {
        Value = value;
    }
    
    public string Value { get; private set; }

    public static Result<DisplayName, Error> Create(string? value)
    {
        var validateResult =  DisplayName
            .Field(value, nameof(Value))
            .NotNullOrWhiteSpace()
            .MaxLength(Constraints.DisplayName.MaxLength)
            .MinLength(Constraints.DisplayName.MinLength)
            .ToViolationsError();
        
        if (validateResult.HasErrors)
        {
            return validateResult;
        }

        return new DisplayName(value!);
    }
    
    public override string ToString() => Value;
    
    public static implicit operator string(DisplayName userName) => userName.Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}