using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.DeleteUser;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class RemoveUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoint.Url.Admins.RemoveUser, async (
                Guid userId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new DeleteUserCommand(userId);
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Admins.RemoveUser)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}