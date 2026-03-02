using System.Net;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.AppDefinitions;

namespace OIO.Application.UserContext.Commands.LoginUser;

public sealed record LoginUserCommand(
    string Account,
    string Password,
    Guid DeviceId,
    IPAddress IpAddress,
    string UserAgent) : ICommand<AuthTokenDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return LoginUserCommand.Check()
            .WithOwnerName("LoginUser")
            .Field(Account)
            .NotWhiteSpace()
            .When(Account.Contains('@'), x => x
                .MaxLength(App.Constraint.UserEmail.MaxLength)
                .Matches(App.Constraint.UserEmail.Regex, message: "Please enter a valid email address."))
            .When(!Account.Contains('@'), x => x
                .MaxLength(App.Constraint.UserName.MaxLength)
                .MinLength(App.Constraint.UserName.MinLength)
                .Matches(App.Constraint.UserName.Regex, message: "User name can only contain alphanumeric characters and dashes."))
            .Field(Password)
            .NotWhiteSpace()
            .Field(UserAgent)
            .NotWhiteSpace();
    }
}