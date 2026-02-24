using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.Context.UserContext.Repositories;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.EnableTwoFactor;

public sealed record EnableTwoFactorCommand(
    string Provider) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return EnableTwoFactorCommand.Check()
            .WithOwnerName("EnableTwoFactor")
            .Field(Provider)
            .NotWhiteSpace()
            .InSet(TwoFactorProvider.All.Select(x => x.Id));
    }
}