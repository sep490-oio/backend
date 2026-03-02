using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.UserContext.Commands.ChangePassword;

namespace OIO.Api.Endpoints.Users;

public class ChangePasswordEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string CurrentPassword,
        [Required] string NewPassword);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Users.ChangePassword, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ChangePasswordCommand(
                    request.CurrentPassword,
                    request.NewPassword);

                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Users.ChangePassword)
            .WithTags(ApiEndpoint.Tags.Users);
    }
}