using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.UpdateProfile;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

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
        app.MapPut(ApiEndpoint.Url.Me.UpdateCurrentUserProfile, async (Request request, ISender sender, CancellationToken ct) =>
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
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.UpdateMyProfile)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}