using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Queries.GetInspectionDashboardStats;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetInspectionDashboardStatsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.InspectionDashboardStats, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetInspectionDashboardStatsQuery(), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetInspectionDashboardStats)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .WithSummary("Get inspection dashboard stats")
            .Produces<InspectionDashboardStatsDto>(StatusCodes.Status200OK);
    }
}
