using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public sealed class AvatarUrl : ValueObject
{
    private AvatarUrl(){}
    private AvatarUrl(string value)
    {
        Value = value;
    }
    
    public string Value { get; private set; }

    public static Result<AvatarUrl, Error> Create(string? value)
    {
        var validateResult = AvatarUrl
            .Check(isInvariant: true)
            .Field(value, nameof(Value))
            .NotNullOrWhiteSpace()
            .MaxLength(AppDefinitions.App.Constraint.AvatarUrl.MaxLength)
            .MinLength(AppDefinitions.App.Constraint.AvatarUrl.MinLength)
            .ToViolationsError();

        if (validateResult.HasErrors)
        {
            return validateResult;
        }
        
        return new AvatarUrl(value!);
    }
    
    public override string ToString() => Value;
    
    public static implicit operator string(AvatarUrl userName) => userName.Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}