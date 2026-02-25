using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.SetPhoneNumber;
using OIO.Domain.Context.UserContext.ValueObjects;

namespace OIO.Api.Endpoints.Users;

public class SetPhoneNumberEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string PhoneNumber,
        string? CountryCode = PhoneNumber.DefaultRegion);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/users/me/phone", async (Request request, ISender sender, CancellationToken ct) =>
            {
                var command = new SetPhoneNumberCommand(
                    request.PhoneNumber,
                    request.CountryCode);

                var result = await sender.Send(command, ct);

                return result.IsFailure ? result.Error.ProcessError() : Results.NoContent();
            })
            .RequireAuthorization()
            .WithTags(Tags.Users);
    }
}