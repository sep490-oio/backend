using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Abstractions.Security;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.SeedWork.Errors;
using OIO.Application.Context.NotificationContext.Commands.MarkNotificationAsRead;

namespace OIO.Api.Endpoints.NotificationContext;

public sealed class MarkAsReadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(ApiEndpoint.Url.Notifications.MarkAsRead, async (
                [FromRoute] Guid notificationId,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new MarkNotificationAsReadCommand(notificationId);
                
                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Notifications.MarkAsRead)
            .WithTags(ApiEndpoint.Tags.Notifications)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
