using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.DisableTwoFactor;

public sealed record DisableTwoFactorCommand(string Code) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return DisableTwoFactorCommand.Check()
            .WithOwnerName("DisableTwoFactor")
            .Field(Code)
            .NotWhiteSpace();
    }
}