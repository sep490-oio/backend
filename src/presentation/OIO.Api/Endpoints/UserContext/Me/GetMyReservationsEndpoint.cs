using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetMyReservations;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class GetMyReservationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyReservations, async (
                ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyReservationsQuery(), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Wallet.GetMyReservations)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<IReadOnlyList<ReservationItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
