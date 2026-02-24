using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.Logout;

namespace OIO.Api.Endpoints.Auth;

public class LogoutUserEndpoint : IEndpoint
{
    public sealed record Request(
        Guid? DeviceId = null);
    
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/logout", async (Request request, ISender sender, CancellationToken ct) =>
            {
                var command = new LogoutCommand(request.DeviceId);
                
                var result = await sender.Send(command, ct);

                return result.IsFailure ? 
                    result.Error.ProcessError() : 
                    Results.Ok();
            })
            .AllowAnonymous()
            .WithTags(Tags.Auth);
    }
}