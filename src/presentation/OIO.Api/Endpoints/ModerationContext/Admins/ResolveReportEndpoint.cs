using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.Commands.ResolveReport;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins;

public sealed class ResolveReportEndpoint : IEndpoint
{
    public sealed record Request(bool Dismissed, string? ResolutionNotes);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Admins.ResolveReport, async (
                Guid reportId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new ResolveReportCommand(reportId, request.Dismissed, request.ResolutionNotes),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ManageItems)
            .WithName(ApiEndpoint.Names.Admins.ResolveReport)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<ReportDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
