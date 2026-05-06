using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.ResolveMonitoringAlert;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins;

public sealed class ResolveMonitoringAlertEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] bool Ignored,
        string? Notes = null,
        string? ResolutionOutcome = null,
        string? ResolutionReason = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.ResolveMonitoringAlert, async (
                Guid alertId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new ResolveMonitoringAlertCommand(
                        alertId,
                        request.Ignored,
                        request.Notes,
                        request.ResolutionOutcome,
                        request.ResolutionReason),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.ResolveMonitoringAlert)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<MonitoringAlertDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
