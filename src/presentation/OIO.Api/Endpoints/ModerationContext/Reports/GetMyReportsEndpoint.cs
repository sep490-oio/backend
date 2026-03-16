using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Queries.GetMyReports;

namespace OIO.Api.Endpoints.ModerationContext.Reports;

public sealed class GetMyReportsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Reports.GetMine, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyReportsQuery(), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Reports.GetMyReports)
            .WithTags(ApiEndpoint.Tags.Reports)
            .Produces<IReadOnlyList<ReportDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
