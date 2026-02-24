using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Commands.ChangeUserStatus;

namespace OIO.Api.Endpoints.Admins;

public class ChangeUserStatusEndpoint : IEndpoint
{
    public sealed record Request(string Status);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("api/admin/users/{userId:guid}/status", async (Guid userId, Request request, ISender sender, CancellationToken ct) =>
            {
                var command = new ChangeUserStatusCommand(userId, request.Status);
                var result = await sender.Send(command, ct);

                return result.IsFailure ? 
                    result.Error.ProcessError() : 
                    Results.NoContent();
            })
            .AllowAnonymous()
            .WithTags(Tags.Admins);
    }
}