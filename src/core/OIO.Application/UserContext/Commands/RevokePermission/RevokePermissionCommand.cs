using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.RevokePermission;

public record RevokePermissionCommand(Guid UserId, int PermissionId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return RevokePermissionCommand.Check()
            .WithOwnerName("RemovePermission")
            .Field(UserId)
            .NotEmptyGuid()
            .Field(PermissionId)
            .NotDefault();
    }
}