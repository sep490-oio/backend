using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetSellerAuctionDeposits;

namespace OIO.Api.Endpoints.PaymentContext.SellerFinance;

public sealed class GetSellerAuctionDepositsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.SellerFinance.AuctionDeposits, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetSellerAuctionDepositsQuery();
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.SellerFinance.GetSellerAuctionDeposits)
            .WithTags(ApiEndpoint.Tags.SellerFinance)
            .Produces<IReadOnlyList<SellerAuctionDepositRowDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
