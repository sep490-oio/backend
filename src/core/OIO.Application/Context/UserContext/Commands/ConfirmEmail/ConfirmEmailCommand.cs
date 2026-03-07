using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(
    Guid UserId,
    string Token) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ConfirmEmailCommand.Check()
            .WithOwnerName("ConfirmEmail")
            .Field(UserId)
            .NotEmptyGuid()
            .Field(Token)
            .NotWhiteSpace();
    }
}