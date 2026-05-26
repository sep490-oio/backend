using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Auctions.Queries.GetMyAuctionStats;

namespace OIO.Api.Endpoints.AuctionContext.Auctions.GetMyAuctionStats;

internal sealed class GetMyAuctionStatsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.MyAuctionStats,
                async (ISender sender, CancellationToken ct) =>
                    (await sender.Send(new GetMyAuctionStatsQuery(), ct)).ToOkHttpResult())
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.GetMyAuctionStats)
            .WithTags(ApiEndpoint.Tags.Me)
            .WithSummary("Get seller auction statistics")
            .Produces<SellerAuctionStatsDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem();
    }
}
