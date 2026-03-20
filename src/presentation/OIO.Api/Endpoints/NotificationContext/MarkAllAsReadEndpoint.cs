using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Abstractions.Security;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.SeedWork.Errors;
using OIO.Application.Context.NotificationContext.Commands.MarkAllNotificationsAsRead;

namespace OIO.Api.Endpoints.NotificationContext;

public sealed class MarkAllAsReadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(ApiEndpoint.Url.Notifications.MarkAllAsRead, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new MarkAllNotificationsAsReadCommand();
                
                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Notifications.MarkAllAsRead)
            .WithTags(ApiEndpoint.Tags.Notifications)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
