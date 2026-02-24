using System.Net;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;
using OIO.Domain.Constants;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

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
                .MaxLength(Constraints.UserEmail.MaxLength)
                .Matches(Constraints.UserEmail.Regex, message: "Please enter a valid email address."))
            .When(!Account.Contains('@'), x => x
                .MaxLength(Constraints.UserName.MaxLength)
                .MinLength(Constraints.UserName.MinLength)
                .Matches(Constraints.UserName.Regex, message: "User name can only contain alphanumeric characters and dashes."))
            .Field(Password)
            .NotWhiteSpace()
            .Field(UserAgent)
            .NotWhiteSpace();
    }
}