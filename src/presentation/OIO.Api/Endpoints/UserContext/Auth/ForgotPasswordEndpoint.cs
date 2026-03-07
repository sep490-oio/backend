using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.ForgotPassword;

namespace OIO.Api.Endpoints.UserContext.Auth;

public sealed class ForgotPasswordEndpoint : IEndpoint
{
    public sealed record Request(string Email);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auth.ForgotPassword, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ForgotPasswordCommand(request.Email);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Auth.ForgotPassword)
            .WithTags(ApiEndpoint.Tags.Auth)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();
    }
}