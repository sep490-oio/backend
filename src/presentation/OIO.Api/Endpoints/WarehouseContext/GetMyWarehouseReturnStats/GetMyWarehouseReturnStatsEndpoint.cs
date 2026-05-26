using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.WarehouseReturns.Queries.GetMyWarehouseReturnStats;

namespace OIO.Api.Endpoints.WarehouseContext.GetMyWarehouseReturnStats;

internal sealed class GetMyWarehouseReturnStatsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.SellerWarehouseReturnStats,
                async (ISender sender, CancellationToken ct) =>
                    (await sender.Send(new GetMyWarehouseReturnStatsQuery(), ct)).ToOkHttpResult())
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Warehouse.GetMyWarehouseReturnStats)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .WithSummary("Get seller warehouse return statistics")
            .Produces<SellerWarehouseReturnStatsDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }
}
