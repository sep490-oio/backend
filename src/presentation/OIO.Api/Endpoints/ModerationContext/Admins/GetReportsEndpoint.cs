using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Queries.GetReports;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins;

public sealed class GetReportsEndpoint : IEndpoint
{
    public sealed record Parameters : GetReportsQueryFilters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetReports, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetReportsQuery(parameters);
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadItems)
            .WithName(ApiEndpoint.Names.Admins.GetReports)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<PagedList<ReportDto>>(StatusCodes.Status200OK);
    }
}