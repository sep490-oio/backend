using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetItemById;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class GetItemByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Items.GetById, async (
                Guid itemId,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetItemByIdQuery(itemId);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Items.GetItemById)
            .WithTags(ApiEndpoint.Tags.Items);
    }
}