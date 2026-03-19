using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.GetNotificationPreference;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class GetNotificationPreferencesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.NotificationPreferences, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetNotificationPreferenceQuery();

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadNotificationPreferences)
            .WithName(ApiEndpoint.Names.Me.GetNotificationPreferences)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}
