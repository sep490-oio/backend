using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.ResetPassword;

namespace OIO.Api.Endpoints.UserContext.Auth;

public sealed class ResetPasswordEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string Email,
        [Required] string Token,
        [Required] string NewPassword,
        [Required] string ConfirmPassword);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Auth.ResetPassword, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ResetPasswordCommand(
                    request.Email,
                    request.Token,
                    request.NewPassword,
                    request.ConfirmPassword);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Auth.ResetPassword)
            .WithTags(ApiEndpoint.Tags.Auth)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem();
    }
}