using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.RefreshToken;
using OIO.Domain.Constants.AppPermissions;

namespace OIO.Api.Endpoints.Auth;

public class RefreshTokenEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string RefreshToken,
        [Required] Guid DeviceId);
    
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/refresh",
                async (HttpContext ctx, Request request, ISender sender, CancellationToken ct) =>
                {
                    var command = new RefreshTokenCommand(
                        request.RefreshToken,
                        request.DeviceId,
                        ctx.GetIpAddress());

                    var result = await sender.Send(command, ct);

                    return result.IsFailure ? result.Error.ProcessError() : Results.Ok(result.Value);
                })
            .WithTags(Tags.Auth)
            .RequireAuthorization(AppPermission.ExpiredTokenAllowed);
    }
}