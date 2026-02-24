using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.UpdateProfile;

namespace OIO.Api.Endpoints.Users;

public class UpdateCurrentUserProfileEndpoint : IEndpoint
{
    public sealed record Request(
        string? FirstName,
        string? LastName,
        string? DisplayName,
        string? AvatarUrl,
        DateOnly? DateOfBirth,
        string? Gender);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/users/me/profile", async (Request request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpdateProfileCommand(
                    request.FirstName,
                    request.LastName,
                    request.DisplayName,
                    request.AvatarUrl,
                    request.DateOfBirth,
                    request.Gender);

                var result = await sender.Send(command, ct);

                return result.IsFailure ? result.Error.ProcessError() : Results.Ok(result.Value);
            })
            .RequireAuthorization()
            .WithTags(Tags.Users);
    }
}