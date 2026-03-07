using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.AssignRole;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class AssignRoleToUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.AssignRole, async (
                Guid userId,
                int roleId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new AssignRoleCommand(userId, roleId);
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Admins.AssignRole)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}