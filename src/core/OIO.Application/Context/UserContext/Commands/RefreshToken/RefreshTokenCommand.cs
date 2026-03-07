using System.Net;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    Guid DeviceId,
    IPAddress IpAddress) : ICommand<AuthTokenDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return RefreshTokenCommand.Check()
            .WithOwnerName("RefreshToken")
            .Field(RefreshToken)
            .NotWhiteSpace()
            .Field(DeviceId)
            .NotEmptyGuid();
    }
}