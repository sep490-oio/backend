using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.EnableTwoFactor;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Users;

public class EnableTwoFactorEndpoint : IEndpoint
{
    public sealed record Request(string Provider);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Users.EnableTwoFactor, async (Request request, ISender sender, CancellationToken ct) =>
            {
                var command = new EnableTwoFactorCommand(request.Provider);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Users.EnableTwoFactor)
            .WithName(ApiEndpoint.Names.Users.EnableTwoFactor)
            .WithTags(ApiEndpoint.Tags.Users);
    }
}