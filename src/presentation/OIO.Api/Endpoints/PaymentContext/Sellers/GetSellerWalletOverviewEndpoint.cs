using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.GetSellerWalletOverview;

namespace OIO.Api.Endpoints.PaymentContext.Sellers;

public sealed class GetSellerWalletOverviewEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Sellers.GetWalletOverview, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSellerWalletOverviewQuery(), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Sellers.GetSellerWalletOverview)
            .WithTags(ApiEndpoint.Tags.Sellers)
            .Produces<SellerWalletOverviewDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
