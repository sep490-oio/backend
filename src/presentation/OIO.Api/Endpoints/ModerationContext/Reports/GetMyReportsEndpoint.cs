using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Queries.GetMyReports;

namespace OIO.Api.Endpoints.ModerationContext.Reports;

public sealed class GetMyReportsEndpoint : IEndpoint
{
    public sealed record Parameters : GetMyReportsQueryFilters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Reports.GetMine, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetMyReportsQuery(parameters);
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Reports.GetMyReports)
            .WithTags(ApiEndpoint.Tags.Reports)
            .Produces<PagedList<ReportDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}