using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.LoginUser;

namespace OIO.Api.Endpoints.Auth;

public class LoginUserEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string Account,
        [Required] string Password,
        Guid DeviceId);
    
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/login", async (HttpContext ctx, HttpRequest httpRequest,  Request request, ISender sender, CancellationToken ct) =>
            {
                var command = new LoginUserCommand(
                    request.Account,
                    request.Password,
                    request.DeviceId,
                    ctx.GetIpAddress(),
                    httpRequest.GetUserAgent());
                
                var result = await sender.Send(command, ct);

                return result.IsFailure ? 
                    result.Error.ProcessError() : 
                    Results.Ok(result.Value);
            })
            .AllowAnonymous()
            .WithTags(Tags.Auth);
    }
}