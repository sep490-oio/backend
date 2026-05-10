using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.DisableTwoFactor;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class DisableTwoFactorEndpoint : IEndpoint
{
    public record DisableTwoFactorRequest(string Code);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.DisableTwoFactor, async (
                DisableTwoFactorRequest request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new DisableTwoFactorCommand(request.Code);
                
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageTwoFactor)
            .WithName(ApiEndpoint.Names.Me.DisableTwoFactor)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}