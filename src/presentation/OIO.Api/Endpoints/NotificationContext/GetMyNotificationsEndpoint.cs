using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Security;
using OIO.Application.Context.NotificationContext.Queries.GetMyNotifications;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Api.Endpoints.NotificationContext;

public sealed class GetMyNotificationsEndpoint : IEndpoint
{
    public sealed record Parameters : PagedParameters;
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Notifications.GetMyNotifications, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetMyNotificationsQuery(parameters);
                
                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Notifications.GetMyNotifications)
            .WithTags(ApiEndpoint.Tags.Notifications)
            .Produces<PagedList<NotificationDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
