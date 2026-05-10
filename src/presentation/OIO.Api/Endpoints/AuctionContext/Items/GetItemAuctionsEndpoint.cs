using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Queries.GetItemAuctions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class GetItemAuctionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Items.GetItemAuctions, async (
                Guid itemId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetItemAuctionsQuery(itemId),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Items.GetItemAuctions)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces<List<AuctionListItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
