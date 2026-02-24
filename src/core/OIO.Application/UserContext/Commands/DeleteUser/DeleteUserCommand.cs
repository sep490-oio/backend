using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.DeleteUser;

public sealed record DeleteUserCommand(Guid UserId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return DeleteUserCommand.Check()
            .WithOwnerName("DeleteUser")
            .Field(UserId)
            .NotEmptyGuid();
    }
}