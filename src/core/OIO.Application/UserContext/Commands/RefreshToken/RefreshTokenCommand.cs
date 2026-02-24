using System.Net;
using CSharpFunctionalExtensions;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.UserContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.UserContext.Commands.RefreshToken;

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