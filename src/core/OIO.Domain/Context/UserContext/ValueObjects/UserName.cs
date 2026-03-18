using CSharpFunctionalExtensions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public sealed class UserName : ValueObject
{
    private UserName(){}
    private UserName(string value)
    {
        Value = value;
        Normalized = value.ToUpperInvariant();
    }
    
    public string Value { get; private set; }
    
    public string Normalized { get; private set; }
    
    public static Result<UserName, ViolationsError> Create(string? value)
    {
        var validateResult =  UserName
            .Check()
            .Field(value, x => x.Value)
            .NotNullOrWhiteSpace()
            .MaxLength(AppDefinitions.App.Constraint.UserName.MaxLength)
            .MinLength(AppDefinitions.App.Constraint.UserName.MinLength)
            .Matches(AppDefinitions.App.Constraint.UserName.Regex, message: "User name can only contain alphanumeric characters and dashes.")
            .ToViolationsError();

        if (validateResult.HasErrors)
        {
            return validateResult;
        }

        return new UserName(value.ToLowerInvariant());
    }
    
    public override string ToString() => Value;
    
    public static implicit operator string(UserName userName) => userName.Value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return Normalized;
    }
}