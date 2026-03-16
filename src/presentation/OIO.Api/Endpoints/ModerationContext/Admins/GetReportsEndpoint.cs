using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Queries.GetReports;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.ModerationContext.Admins;

public sealed class GetReportsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetReports, async (
                string? status,
                string? entityType,
                Guid? entityId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetReportsQuery(status, entityType, entityId),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadItems)
            .WithName(ApiEndpoint.Names.Admins.GetReports)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces<IReadOnlyList<ReportDto>>(StatusCodes.Status200OK);
    }
}
