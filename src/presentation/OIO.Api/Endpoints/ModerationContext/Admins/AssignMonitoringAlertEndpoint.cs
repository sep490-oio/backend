using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.AssignMonitoringAlert;
using OIO.Application.Context.ModerationContext.Commands.UnassignMonitoringAlert;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins;

public sealed class AssignMonitoringAlertEndpoint : IEndpoint
{
    public sealed record Request([Required] Guid AssignToUserId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.AssignMonitoringAlert, async (
                Guid alertId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AssignMonitoringAlertCommand(alertId, request.AssignToUserId),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.AssignMonitoringAlert)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<MonitoringAlertDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapDelete(ApiEndpoint.Url.Admins.UnassignMonitoringAlert, async (
                Guid alertId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new UnassignMonitoringAlertCommand(alertId),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.UnassignMonitoringAlert)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<MonitoringAlertDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
