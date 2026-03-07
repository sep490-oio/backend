using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.TogglePermissionInRole;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class TogglePermissionFromRoleEndpoint : IEndpoint
{
    public sealed record Request( [Required] bool IsActive);
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Admins.TogglePermission, async (
                int roleId,
                int permissionId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new TogglePermissionCommand(roleId, permissionId, request.IsActive);
                
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManagePermissions)
            .WithName(ApiEndpoint.Names.Admins.TogglePermission)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}