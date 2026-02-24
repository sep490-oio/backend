using CSharpFunctionalExtensions;
using OIO.Domain.Constants;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public sealed class UserEmail : ValueObject
{
    private UserEmail(string value)
    {
        Value = value;
        Normalized = value.ToUpperInvariant();
    }
    
    public string Value { get; private set; }
    
    public string Normalized { get; private set; }

    public static Result<UserEmail, Error> Create(string value)
    {
        var validateResult = UserEmail
            .Field(value, nameof(value))!
            .NotNullOrWhiteSpace()
            .MaxLength(Constraints.UserEmail.MaxLength)
            .Matches(Constraints.UserEmail.Regex, message: "Please enter a valid email address.")
            .ToViolationsError();
        
        if (validateResult.HasErrors)
        {
            return validateResult;
        }

        return new UserEmail(value.ToLowerInvariant());
    }
    
    public override string ToString() => Value;
    
    public static implicit operator string(UserEmail email) => email.Value;
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        yield return Normalized;
    }
}