using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.UnlockUser;

namespace OIO.Api.Endpoints.Admins;

public class UnlockUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("api/admin/users/{userId:guid}/unlock", async (Guid userId, ISender sender, CancellationToken ct) =>
            {
                var command = new UnlockUserCommand(userId);
                var result = await sender.Send(command, ct);

                return result.IsFailure ? 
                    result.Error.ProcessError() : 
                    Results.NoContent();
            })
            .AllowAnonymous()
            .WithTags(Tags.Admins);
    }
}