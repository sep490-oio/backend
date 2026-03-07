using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.RevokePermission;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class RevokePermissionFromUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoint.Url.Admins.RevokePermission, async (
                Guid userId,
                int permissionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new RevokePermissionCommand(userId, permissionId);
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.RevokePermission)
            .WithName(ApiEndpoint.Names.Admins.RevokePermission)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}