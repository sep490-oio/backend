using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Context.UserContext.Commands.RegenerateRecoveryCodes;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class RegenerateRecoveryCodesEndpoint : IEndpoint
{
    public sealed record Request([Required] string Code);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.RegenerateRecoveryCodes, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new RegenerateRecoveryCodesCommand(request.Code);

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageTwoFactor)
            .WithName(ApiEndpoint.Names.Me.RegenerateRecoveryCodes)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}
