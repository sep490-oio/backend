using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.GrantPermission;

namespace OIO.Api.Endpoints.Admins;

public class GrantPermissionForUser : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/admin/users/permissions/{permissionId:guid}", async (Guid userId, Guid permissionId, ISender sender, CancellationToken ct) =>
            {
                var command = new GrantPermissionCommand(userId, permissionId);
                var result = await sender.Send(command, ct);

                return result.IsFailure ? 
                    result.Error.ProcessError() : 
                    Results.NoContent();
            })
            .AllowAnonymous()
            .WithTags(Tags.Admins);
    }
}