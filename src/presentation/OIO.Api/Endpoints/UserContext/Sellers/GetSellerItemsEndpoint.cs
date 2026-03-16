using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetPublicSellerItems;

namespace OIO.Api.Endpoints.UserContext.Sellers;

public sealed class GetSellerItemsEndpoint : IEndpoint
{
    public sealed record Parameters : GetPublicSellerItemsFilterParameters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Sellers.GetItems, async (
                Guid sellerId,
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetPublicSellerItemsQuery(sellerId, parameters);
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Sellers.GetSellerItems)
            .WithTags(ApiEndpoint.Tags.Sellers);
    }
}
