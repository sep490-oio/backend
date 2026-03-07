using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.ChangeUserStatus;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class ChangeUserStatusEndpoint : IEndpoint
{
    public sealed record Request(string Status);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(ApiEndpoint.Url.Admins.ChangeUserStatus, async (
                Guid userId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new ChangeUserStatusCommand(userId, request.Status);
                var result = await sender.Send(command, ct);

                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Admins.ChangeUserStatus)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}