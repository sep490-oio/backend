using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.ChangePassword;

namespace OIO.Api.Endpoints.Users;

public class ChangePasswordEndpoint : IEndpoint
{
    public sealed record Request(
        string CurrentPassword,
        string NewPassword);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/users/me/password", async (Request request, ISender sender, CancellationToken ct) =>
            {
                var command = new ChangePasswordCommand(
                    request.CurrentPassword,
                    request.NewPassword);

                var result = await sender.Send(command, ct);

                return result.IsFailure ? result.Error.ProcessError() : Results.NoContent();
            })
            .RequireAuthorization()
            .WithTags(Tags.Users);
    }
}