using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.ResendConfirmEmail;

namespace OIO.Api.Endpoints.UserContext.Auth;

public sealed class ResendConfirmEmailEndpoint : IEndpoint
{
    public sealed record Request([Required] string Email);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auth.ResendConfirmEmail, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ResendConfirmEmailCommand(request.Email);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Auth.ResendConfirmEmail)
            .WithTags(ApiEndpoint.Tags.Auth)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();
    }
}