using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.RevokeRole;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

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
                var command = new RevokeRoleCommand(userId, roleId);
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.RevokeRole)
            .WithName(ApiEndpoint.Names.Admins.RevokeRole)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}