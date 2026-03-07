using System.ComponentModel.DataAnnotations;
using System.Net;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.ConfirmEmail;

namespace OIO.Api.Endpoints.UserContext.Auth;

public class ConfirmEmailEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] Guid UserId,
        [Required] string Token);
    
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auth.ConfirmEmail, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ConfirmEmailCommand(request.UserId ,request.Token);
                
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Auth.ConfirmEmail)
            .WithTags(ApiEndpoint.Tags.Auth)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }
}