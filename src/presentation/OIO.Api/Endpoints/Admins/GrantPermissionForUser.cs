using MediatR;
using OIO.Api.Common;
using OIO.Application.UserContext.Commands.GrantPermission;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Admins;

public class GrantPermissionForUser : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.GrantPermission, async (
                Guid userId,
                int permissionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new GrantPermissionCommand(userId, permissionId);
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Users.GrantPermissions)
            .WithName(ApiEndpoint.Names.Admins.GrantPermission)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}