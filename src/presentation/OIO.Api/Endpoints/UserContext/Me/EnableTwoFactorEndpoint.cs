using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.EnableTwoFactor;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class EnableTwoFactorEndpoint : IEndpoint
{
    public sealed record Request([Required] string Provider);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.EnableTwoFactor, async (Request request, ISender sender, CancellationToken ct) =>
            {
                var command = new EnableTwoFactorCommand(request.Provider);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageTwoFactor)
            .WithName(ApiEndpoint.Names.Me.EnableTwoFactor)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}