using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Queries.GetMonitoringAlerts;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins;

public sealed class GetMonitoringAlertsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetMonitoringAlerts, async (
                string? status,
                string? entityType,
                Guid? entityId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetMonitoringAlertsQuery(status, entityType, entityId),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadItems)
            .WithName(ApiEndpoint.Names.Admins.GetMonitoringAlerts)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<IReadOnlyList<MonitoringAlertDto>>(StatusCodes.Status200OK);
    }
}
