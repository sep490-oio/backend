using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.DisableTwoFactor;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class DisableTwoFactorEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.DisableTwoFactor, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new DisableTwoFactorCommand();
                
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.DisableTwoFactor)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}