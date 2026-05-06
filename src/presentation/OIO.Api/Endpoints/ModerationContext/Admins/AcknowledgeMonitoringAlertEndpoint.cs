using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.AcknowledgeMonitoringAlert;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins;

public sealed class AcknowledgeMonitoringAlertEndpoint : IEndpoint
{
    public sealed record Request(string? Notes, bool AssignToMe = false);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.AcknowledgeMonitoringAlert, async (
                Guid alertId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AcknowledgeMonitoringAlertCommand(alertId, request.Notes, request.AssignToMe),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.AcknowledgeMonitoringAlert)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<MonitoringAlertDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
