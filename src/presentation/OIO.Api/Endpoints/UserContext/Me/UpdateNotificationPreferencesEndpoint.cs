using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Commands.UpdateNotificationPreference;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class UpdateNotificationPreferencesEndpoint : IEndpoint
{
    public sealed record Request(
        bool IsEnabled,
        string Channels,
        string? QuietHours);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Me.NotificationPreferences, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new UpdateNotificationPreferenceCommand(
                    request.IsEnabled,
                    request.Channels,
                    request.QuietHours);

                var result = await sender.Send(command, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ManageNotificationPreferences)
            .WithName(ApiEndpoint.Names.Me.UpdateNotificationPreferences)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}
