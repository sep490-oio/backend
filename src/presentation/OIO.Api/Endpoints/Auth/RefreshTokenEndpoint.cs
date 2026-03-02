using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.RefreshToken;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Auth;

public class RefreshTokenEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string RefreshToken,
        [Required] Guid DeviceId);
    
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auth.RefreshToken, async (
                HttpContext ctx, 
                Request request,
                ISender sender,
                CancellationToken ct) =>
                {
                    var command = new RefreshTokenCommand(
                        request.RefreshToken,
                        request.DeviceId,
                        ctx.GetIpAddress());

                    var result = await sender.Send(command, ct);
                    
                    return result.ToOkHttpResult();
                })
            .RequireAuthorization(App.Policy.ExpiredTokenAllowed)
            .WithName(ApiEndpoint.Names.Auth.RefreshToken)
            .WithTags(ApiEndpoint.Tags.Auth);
    }
}