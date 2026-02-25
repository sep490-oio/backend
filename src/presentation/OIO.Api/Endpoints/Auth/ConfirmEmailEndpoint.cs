using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.ConfirmEmail;

namespace OIO.Api.Endpoints.Auth;

public class ConfirmEmailEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] Guid UserId, 
        [Required] string Token);
    
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/confirm-email", async (Request request, ISender sender, CancellationToken ct) =>
            {
                var command = new ConfirmEmailCommand(request.UserId, request.Token);
                
                var result = await sender.Send(command, ct);

                return result.IsFailure ? 
                    result.Error.ProcessError() : 
                    Results.Ok();
            })
            .AllowAnonymous()
            .WithTags(Tags.Auth);
    }
}