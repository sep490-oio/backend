using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetMyPendingWinnerOffers;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class GetMyPendingWinnerOffersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.MyPendingWinnerOffers, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetMyPendingWinnerOffersQuery();

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadBids)
            .WithName(ApiEndpoint.Names.Me.GetMyPendingWinnerOffers)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}
