using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetAuctions;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class GetAuctionsEndpoint : IEndpoint
{
    public sealed record Parameters : GetAuctionsFilterParameters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Auctions.GetAll, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetAuctionsQuery(parameters);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Auctions.GetAllAuctions)
            .WithTags(ApiEndpoint.Tags.Auctions);
    }
}