using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.DeleteUser;

namespace OIO.Api.Endpoints.Admins;

public class DeleteUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/admin/users/{userId:guid}", async (Guid userId, ISender sender, CancellationToken ct) =>
            {
                var command = new DeleteUserCommand(userId);
                var result = await sender.Send(command, ct);

                return result.IsFailure ? 
                    result.Error.ProcessError() : 
                    Results.NoContent();
            })
            .AllowAnonymous()
            .WithTags(Tags.Admins);
    }
}