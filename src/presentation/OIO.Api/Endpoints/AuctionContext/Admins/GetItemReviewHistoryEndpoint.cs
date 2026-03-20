using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetItemReviewHistory;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Admins;

public sealed class GetItemReviewHistoryEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetItemReviewHistory, async (
                Guid itemId,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetItemReviewHistoryQuery(itemId);
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadItems)
            .WithName(ApiEndpoint.Names.Admins.GetItemReviewHistory)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status200OK);
    }
}
