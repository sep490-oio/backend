using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.UpdateProfile;
using OIO.Domain.AppDefinitions;

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
        app.MapPut(ApiEndpoint.Url.Users.UpdateCurrentUserProfile, async (Request request, ISender sender, CancellationToken ct) =>
            {
                var command = new UpdateProfileCommand(
                    request.FirstName,
                    request.LastName,
                    request.DisplayName,
                    request.AvatarUrl,
                    request.DateOfBirth,
                    request.Gender);

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Users.UpdateMe)
            .WithName(ApiEndpoint.Names.Users.UpdateCurrentUserProfile)
            .WithTags(ApiEndpoint.Tags.Users);
    }
}