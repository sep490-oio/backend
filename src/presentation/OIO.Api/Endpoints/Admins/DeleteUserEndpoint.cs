using MediatR;
using OIO.Api.Common;
using OIO.Application.UserContext.Commands.DeleteUser;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Admins;

public class RemoveUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoint.Url.Admins.DeleteUser, async (
                Guid userId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new DeleteUserCommand(userId);
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Users.Remove)
            .WithName(ApiEndpoint.Names.Admins.DeleteUser)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}