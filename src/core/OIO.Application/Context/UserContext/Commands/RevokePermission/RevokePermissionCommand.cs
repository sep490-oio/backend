using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.RevokePermission;

public record RevokePermissionCommand(Guid UserId, string Permission) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return RevokePermissionCommand.Check()
            .WithOwnerName("RemovePermission")
            .Field(UserId)
            .NotEmptyGuid()
            .Field(Permission)
            .NotWhiteSpace();
    }
}