using OIO.Application.Abstractions.Messaging;
using OIO.Domain.Context.UserContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.ChangeUserStatus;

public sealed record ChangeUserStatusCommand(
    Guid UserId,
    string NewStatus) : ICommand, IHasValidate
{
    public ViolationsError Validate()
    {
        return ChangeUserStatusCommand.Check()
            .WithOwnerName("ChangeUserStatus")
            .Field(UserId)
            .NotEmptyGuid()
            .Field(NewStatus)
            .NotWhiteSpace()
            .InSet(UserStatus.All.Select(status => status.Id));
    }
}