using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.Orders.Queries.GetMyOrderStats;

namespace OIO.Api.Endpoints.OrderContext.Orders.GetMyOrderStats;

internal sealed class GetMyOrderStatsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyOrderStats,
                async (ISender sender, CancellationToken ct) =>
                    (await sender.Send(new GetMyOrderStatsQuery(), ct)).ToOkHttpResult())
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.GetMyOrderStats)
            .WithTags(ApiEndpoint.Tags.Me)
            .WithSummary("Get seller order statistics")
            .Produces<SellerOrderStatsDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }
}
