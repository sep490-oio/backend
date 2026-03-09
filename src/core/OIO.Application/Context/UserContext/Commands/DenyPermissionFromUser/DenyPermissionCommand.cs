using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.DenyPermissionFromUser;

public record DenyPermissionCommand(Guid UserId, string Permission) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return DenyPermissionCommand
            .Check("DenyPermission")
            .Field(UserId)
            .NotEmptyGuid()
            .Field(Permission)
            .NotWhiteSpace();
    }
}