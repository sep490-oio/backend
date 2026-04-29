using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.GetSellerFinanceOverview;

namespace OIO.Api.Endpoints.PaymentContext.SellerFinance;

public sealed class GetSellerFinanceOverviewEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.SellerFinance.Overview, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSellerFinanceOverviewQuery(), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.SellerFinance.GetSellerFinanceOverview)
            .WithTags(ApiEndpoint.Tags.SellerFinance)
            .Produces<SellerFinanceOverviewDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
