using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetMyAutoBid;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class GetMyAutoBidEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Auctions.GetMyAutoBid, async (
                Guid auctionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetMyAutoBidQuery(auctionId);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Auctions.GetMyAutoBid)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}