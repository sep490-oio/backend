using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetMyActiveDeposits;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class GetMyActiveDepositsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyActiveDeposits, async (
                ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyActiveDepositsQuery(), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Wallet.GetMyActiveDeposits)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<IReadOnlyList<ActiveDepositDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
