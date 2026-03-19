using OIO.Application.Abstractions.Messaging;
using OIO.Domain.AppDefinitions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using appCurrency = OIO.Domain.Context.Shared.Enums.Currency;

namespace OIO.Application.Context.UserContext.Commands.AdminCreateUser;

public sealed record AdminCreateUserCommand(
    string UserName,
    string Email,
    string? Password,
    string Currency,
    string FirstName,
    string LastName,
    string? DisplayName,
    IReadOnlyList<string>? Roles,
    bool EmailConfirmed = false,
    bool SkipNotifications = false
) : ICommand<AdminUserCreatedDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return AdminCreateUserCommand
            .Check()
            .WithOwnerName("AdminCreateUser")
            .Field(Email)
            .NotWhiteSpace()
            .MaxLength(App.Constraint.UserEmail.MaxLength)
            .Matches(App.Constraint.UserEmail.Regex)
            .Field(UserName)
            .NotWhiteSpace()
            .MinLength(App.Constraint.UserName.MinLength)
            .MaxLength(App.Constraint.UserName.MaxLength)
            .Matches(App.Constraint.UserName.Regex)
            .Field(Password)
            .When(Password is not null, x =>
                x.NotNullOrWhiteSpace()
                .MinLength(App.Constraint.Password.MinLength)
                .MaxLength(App.Constraint.Password.MaxLength)
                .Format(
                    message: App.Constraint.Password.FormatMessage,
                    validators: App.Constraint.Password.Validator)!)
            .Field(FirstName)
            .When(FirstName is not null, x =>
                x.NotNullOrWhiteSpace()
                .MaxLength(App.Constraint.FirstName.MaxLength)
                .MinLength(App.Constraint.FirstName.MinLength)!)
            .Field(LastName)
            .When(LastName is not null, x =>
                x.NotNullOrWhiteSpace()
                .MaxLength(App.Constraint.LastName.MaxLength)
                .MinLength(App.Constraint.LastName.MinLength)!)
            .Field(Currency)
            .InSet(appCurrency.All.Select(g => g.Id), error: appCurrency.Errors.NotSupported);
    }
}

public sealed record AdminUserCreatedDto(
    Guid UserId,
    string UserName,
    string Email,
    string Status,
    IReadOnlyList<string> Roles,
    bool EmailConfirmed,
    string? TemporaryPassword);
