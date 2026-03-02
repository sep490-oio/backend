using MediatR;
using OIO.Api.Common;
using OIO.Application.UserContext.Commands.UnlockUser;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Admins;

public class UnlockUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(ApiEndpoint.Url.Admins.UnlockUser, async (Guid userId, ISender sender, CancellationToken ct) =>
            {
                var command = new UnlockUserCommand(userId);
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Users.Unlock)
            .WithName(ApiEndpoint.Names.Admins.UnlockUser)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}