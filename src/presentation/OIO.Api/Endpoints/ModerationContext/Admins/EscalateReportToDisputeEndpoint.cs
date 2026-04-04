using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.EscalateReportToDispute;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins;

public sealed class EscalateReportToDisputeEndpoint : IEndpoint
{
    public sealed record Request(
        string? Title = null,
        string? DisputeType = null,
        string? Priority = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.EscalateReportToDispute, async (
                Guid reportId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new EscalateReportToDisputeCommand(reportId, request.Title, request.DisputeType, request.Priority),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.EscalateReportToDispute)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<DisputeThreadMetaDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
