using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetAuctionById;

namespace OIO.Api.Endpoints.AuctionContext.Auctions;

public sealed class GetAuctionByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Auctions.GetById, async (
                Guid auctionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetAuctionByIdQuery(auctionId);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Auctions.GetAuctionById)
            .WithTags(ApiEndpoint.Tags.Auctions)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}