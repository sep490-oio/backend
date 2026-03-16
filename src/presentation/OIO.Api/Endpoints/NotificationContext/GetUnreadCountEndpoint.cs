using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.NotificationContext.Queries.GetUnreadNotificationCount;

namespace OIO.Api.Endpoints.NotificationContext;

public sealed class GetUnreadCountEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Notifications.GetUnreadCount, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetUnreadNotificationCountQuery();
                
                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Notifications.GetUnreadCount)
            .WithTags(ApiEndpoint.Tags.Notifications)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
