using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.GrantPermission;

public sealed record GrantPermissionCommand(
    Guid UserId,
    int PermissionId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return GrantPermissionCommand.Check()
            .WithOwnerName("GrantPermission")
            .Field(UserId)
            .NotEmptyGuid()
            .Field(PermissionId)
            .NotDefault();
    }
}