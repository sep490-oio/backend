using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetMyItems;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class GetMyItemsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Items.GetBySeller, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetMyItemsQuery();

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Items.GetMyItems)
            .WithTags(ApiEndpoint.Tags.Items)
            .Produces(StatusCodes.Status200OK);
    }
}