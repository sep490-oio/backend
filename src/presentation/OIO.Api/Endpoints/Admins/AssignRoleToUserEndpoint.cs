using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.AssignRole;

namespace OIO.Api.Endpoints.Admins;

public class AssignRoleToUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/admin/users/roles/{roleId:guid}", async (Guid userId, Guid roleId, ISender sender, CancellationToken ct) =>
            {
                var command = new AssignRoleCommand(userId, roleId);
                var result = await sender.Send(command, ct);

                return result.IsFailure ? 
                    result.Error.ProcessError() : 
                    Results.NoContent();
            })
            .AllowAnonymous()
            .WithTags(Tags.Admins);
    }
}