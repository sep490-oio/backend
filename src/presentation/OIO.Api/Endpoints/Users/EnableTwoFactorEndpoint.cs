using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.EnableTwoFactor;

namespace OIO.Api.Endpoints.Users;

public class EnableTwoFactorEndpoint : IEndpoint
{
    public sealed record Request(string Provider);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/users/me/two-factor/enable", async (Request request, ISender sender, CancellationToken ct) =>
            {
                var command = new EnableTwoFactorCommand(request.Provider);

                var result = await sender.Send(command, ct);

                return result.IsFailure ? result.Error.ProcessError() : Results.NoContent();
            })
            .RequireAuthorization()
            .WithTags(Tags.Users);
    }
}