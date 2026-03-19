using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.EscalateReportEmergency;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins;

public sealed class EscalateReportEmergencyEndpoint : IEndpoint
{
    public sealed record Request(string? ReasonOverride = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.EscalateReportEmergency, async (
                Guid reportId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new EscalateReportEmergencyCommand(reportId, request.ReasonOverride),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.EscalateReportEmergency)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<ReportDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
