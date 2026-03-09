using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.TogglePermissionInRole;

public record TogglePermissionCommand(string Role, string Permission, bool IsActive) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return TogglePermissionCommand
            .Check("InactivePermission")
            .Field(Role)
            .NotWhiteSpace()
            .Field(Permission)
            .NotWhiteSpace();
    }
}