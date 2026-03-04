using MediatR;
using OIO.Api.Common;
using OIO.Application.UserContext.Commands.DenyPermissionFromUser;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Admins;

public class DenyPermissionFromUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Admins.DenyPermission, async (
                Guid userId,
                int permissionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new DenyPermissionCommand(userId, permissionId);
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Users.DenyPermissions)
            .WithName(ApiEndpoint.Names.Admins.DenyPermission)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}