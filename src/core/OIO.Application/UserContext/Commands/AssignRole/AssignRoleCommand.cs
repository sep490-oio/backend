using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.AssignRole;

public sealed record AssignRoleCommand(
    Guid UserId,
    int RoleId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return AssignRoleCommand.Check()
            .WithOwnerName("AssignRole")
            .Field(UserId)
            .NotEmptyGuid()
            .Field(RoleId)
            .NotDefault();
    }
}