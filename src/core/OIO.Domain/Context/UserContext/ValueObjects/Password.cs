using CSharpFunctionalExtensions;
using OIO.Domain.Context.UserContext.Services;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Domain.Context.UserContext.ValueObjects;

public sealed class Password : ValueObject
{
    private Password() {}
    private Password(string hashedValue) => HashedValue = hashedValue;
    
    public string HashedValue { get; private set; }
    
    public static Result<Password, Error> Create(string plainPassword, IPasswordHasher passwordHasher)
    {
        var validateResult = Password
            .Field(plainPassword, "Value")!
            .NotNullOrWhiteSpace()
            .MaxLength(AppDefinitions.App.Constraint.Password.MaxLength)
            .MinLength(AppDefinitions.App.Constraint.Password.MinLength)
            .Format(
                message: AppDefinitions.App.Constraint.Password.FormatMessage,
                validators: AppDefinitions.App.Constraint.Password.Validator)
            .ToViolationsError();

        if (validateResult.HasErrors)
            return validateResult;

        var hashed = passwordHasher.Hash(plainPassword);
        return new Password(hashed);
    }
    
    public static Password CreateFromHash(string hashedPassword) => new(hashedPassword);
    
    public bool Verify(string plainPassword, IPasswordHasher passwordHasher) 
        => passwordHasher.Verify(plainPassword, HashedValue);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return HashedValue;
    }
}