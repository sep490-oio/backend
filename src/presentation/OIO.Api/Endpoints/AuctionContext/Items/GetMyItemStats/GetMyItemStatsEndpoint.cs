using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Items.Queries.GetMyItemStats;

namespace OIO.Api.Endpoints.AuctionContext.Items.GetMyItemStats;

internal sealed class GetMyItemStatsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Items.GetMyItemStats,
                async (ISender sender, CancellationToken ct) =>
                    (await sender.Send(new GetMyItemStatsQuery(), ct)).ToOkHttpResult())
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Items.GetMyItemStats)
            .WithTags(ApiEndpoint.Tags.Items)
            .WithSummary("Get seller item statistics")
            .Produces<SellerItemStatsDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }
}
