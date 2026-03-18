using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetReviewQueue;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.AuctionContext.Admins;

public sealed class GetReviewQueueEndpoint : IEndpoint
{
    public sealed record Parameters : GetReviewQueueQueryFilterParameters;
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetItemReviewQueue, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetReviewQueueQuery(
                    Parameters: parameters);

                var result = await sender.Send(query, ct);
                
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadItems)
            .WithName(ApiEndpoint.Names.Admins.GetItemReviewQueue)
            .WithTags(ApiEndpoint.Tags.Admins)
            .Produces(StatusCodes.Status200OK);
    }
}
