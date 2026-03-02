using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public sealed class LastName : ValueObject
{
    private LastName(){}
    private LastName(string value)
    {
        Value = value;
    }
    
    public string Value { get; private set; }

    public static Result<LastName, Error> Create(string? value)
    {
        var validateResult = LastName
            .Field(value, nameof(Value))!
            .NotNullOrWhiteSpace()
            .MaxLength(AppDefinitions.App.Constraint.LastName.MaxLength)
            .MinLength(AppDefinitions.App.Constraint.LastName.MinLength)
            .ToViolationsError();

        if (validateResult.HasErrors)
        {
            return validateResult;
        }
        
        return new LastName(value);
    }
    
    public override string ToString() => Value;
    
    public static implicit operator string(LastName userName) => userName.Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}