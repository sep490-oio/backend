using MediatR;
using OIO.Api.Common;
using OIO.Application.UserContext.Commands.RemoveRole;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Admins;

public class RevokeRoleFromUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoint.Url.Admins.RevokeRole, async (
                Guid userId,
                int roleId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new RemoveRoleCommand(userId, roleId);
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Users.RevokeRoles)
            .WithName(ApiEndpoint.Names.Admins.RevokeRole)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}