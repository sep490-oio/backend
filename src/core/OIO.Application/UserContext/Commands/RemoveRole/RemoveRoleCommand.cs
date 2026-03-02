using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.RemoveRole;

public sealed record RemoveRoleCommand(
    Guid UserId,
    int RoleId) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return RemoveRoleCommand.Check()
            .WithOwnerName("RemoveRole")
            .Field(UserId)
            .NotEmptyGuid()
            .Field(RoleId)
            .NotDefault();
    }
}