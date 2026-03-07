using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.Filters;
using OIO.Application.Context.AuctionContext.Queries.GetMyWatchlist;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class GetMyAuctionWatchlistEndpoint : IEndpoint
{
    public sealed record Parameters : MyWatchlistFilterParameters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.MyAuctionWatchlist, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetMyAuctionWatchlistQuery(parameters);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.GetMyAuctionWatchlist)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}