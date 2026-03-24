using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetPublicItems;

namespace OIO.Api.Endpoints.AuctionContext.Items;

public sealed class GetPublicItemsEndpoint : IEndpoint
{
    public sealed record Parameters : GetPublicItemsFilterParameters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Items.GetPublic, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetPublicItemsQuery(parameters);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Items.GetPublicItems)
            .WithTags(ApiEndpoint.Tags.Items);
    }
}
