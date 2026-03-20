using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Context.UserContext.Commands.LoginUser;

namespace OIO.Api.Endpoints.UserContext.Auth;

public class LoginUserEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string Account,
        [Required] string Password,
        [Required] Guid DeviceId);
    
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auth.Login, async (
                HttpContext ctx, 
                HttpRequest httpRequest,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new LoginUserCommand(
                    request.Account,
                    request.Password,
                    request.DeviceId,
                    ctx.GetIpAddress(),
                    httpRequest.GetUserAgent());
                
                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Auth.Login)
            .WithTags(ApiEndpoint.Tags.Auth);
    }
}