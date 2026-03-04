using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.DenyPermissionFromUser;

public record DenyPermissionCommand(Guid UserId, int PermissionId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return DenyPermissionCommand
            .Check("DenyPermission")
            .Field(UserId)
            .NotEmptyGuid()
            .Field(PermissionId)
            .NotDefault();
    }
}