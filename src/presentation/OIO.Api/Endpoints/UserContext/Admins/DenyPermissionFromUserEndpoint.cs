using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.DenyPermissionFromUser;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class DenyPermissionFromUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Admins.DenyPermission, async (
                Guid userId,
                string permission,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new DenyPermissionCommand(userId, permission);
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.DenyPermission)
            .WithName(ApiEndpoint.Names.Admins.DenyPermission)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}