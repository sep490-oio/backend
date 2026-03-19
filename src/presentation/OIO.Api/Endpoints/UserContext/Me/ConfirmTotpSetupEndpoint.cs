using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.ConfirmTotpSetup;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class ConfirmTotpSetupEndpoint : IEndpoint
{
    public sealed record Request([Required] string Code);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.ConfirmTotpSetup, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ConfirmTotpSetupCommand(request.Code);

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageTwoFactor)
            .WithName(ApiEndpoint.Names.Me.ConfirmTotpSetup)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}
