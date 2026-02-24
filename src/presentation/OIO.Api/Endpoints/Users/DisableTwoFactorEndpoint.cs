using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.DisableTwoFactor;

namespace OIO.Api.Endpoints.Users;

public class DisableTwoFactorEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/users/me/two-factor/disable", async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new DisableTwoFactorCommand(), ct);

                return result.IsFailure ? result.Error.ProcessError() : Results.NoContent();
            })
            .RequireAuthorization()
            .WithTags(Tags.Users);
    }
}