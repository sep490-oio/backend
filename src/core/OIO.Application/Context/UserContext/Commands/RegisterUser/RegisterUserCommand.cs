using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.AppDefinitions;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using appCurrency = OIO.Domain.Context.Shared.Enums.Currency;

namespace OIO.Application.Context.UserContext.Commands.RegisterUser;

public sealed record RegisterUserCommand(
    string UserName,
    string Email,
    string Password,
    string Currency = "VND",
    string? FirstName = null,
    string? LastName = null) : ICommand<UserDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return RegisterUserCommand
            .Check()
            .WithOwnerName("RegisterUser")
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
            .NotWhiteSpace()
            .MinLength(App.Constraint.Password.MinLength)
            .MaxLength(App.Constraint.Password.MaxLength)
            .Format(
                message: App.Constraint.Password.FormatMessage,
                validators: App.Constraint.Password.Validator)
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