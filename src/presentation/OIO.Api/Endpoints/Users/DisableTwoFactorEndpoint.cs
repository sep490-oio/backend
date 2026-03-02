using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.DisableTwoFactor;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Users;

public class DisableTwoFactorEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Users.DisableTwoFactor, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new DisableTwoFactorCommand();
                
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Users.DisableTwoFactor)
            .WithName(ApiEndpoint.Names.Users.DisableTwoFactor)
            .WithTags(ApiEndpoint.Tags.Users);
    }
}