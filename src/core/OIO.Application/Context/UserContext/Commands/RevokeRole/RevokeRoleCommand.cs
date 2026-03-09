using OIO.Application.Abstractions.Messaging;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.RevokeRole;

public sealed record RevokeRoleCommand(
    Guid UserId,
    string Role) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return RevokeRoleCommand.Check()
            .WithOwnerName("RemoveRole")
            .Field(UserId)
            .NotEmptyGuid()
            .Field(Role)
            .NotWhiteSpace();
    }
}