using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.SetupTotp;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class SetupTotpEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.SetupTotp, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new SetupTotpCommand();

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageTwoFactor)
            .WithName(ApiEndpoint.Names.Me.SetupTotp)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}
