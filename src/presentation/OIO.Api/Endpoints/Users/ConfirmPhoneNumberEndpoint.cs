using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.ConfirmPhoneNumber;

namespace OIO.Api.Endpoints.Users;

public class ConfirmPhoneNumberEndpoint : IEndpoint
{
    public sealed record Request(string VerificationCode);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/users/me/phone/confirm", async (Request request, ISender sender, CancellationToken ct) =>
            {
                var command = new ConfirmPhoneNumberCommand(request.VerificationCode);

                var result = await sender.Send(command, ct);

                return result.IsFailure ? result.Error.ProcessError() : Results.NoContent();
            })
            .RequireAuthorization()
            .WithTags(Tags.Users);
    }
}