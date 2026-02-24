using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.RemoveRole;

namespace OIO.Api.Endpoints.Admins;

public class RemoveRoleFromUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/admin/users/roles/{roleId:guid}", async (Guid userId, Guid roleId, ISender sender, CancellationToken ct) =>
            {
                var command = new RemoveRoleCommand(userId, roleId);
                var result = await sender.Send(command, ct);

                return result.IsFailure ? 
                    result.Error.ProcessError() : 
                    Results.NoContent();
            })
            .AllowAnonymous()
            .WithTags(Tags.Admins);
    }
}