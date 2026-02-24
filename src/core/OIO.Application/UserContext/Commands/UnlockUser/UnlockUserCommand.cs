using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.UnlockUser;

public sealed record UnlockUserCommand(Guid UserId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return UnlockUserCommand
            .Check()
            .WithOwnerName("UnlockUser")
            .Field(UserId)
            .NotEmptyGuid();
    }
}