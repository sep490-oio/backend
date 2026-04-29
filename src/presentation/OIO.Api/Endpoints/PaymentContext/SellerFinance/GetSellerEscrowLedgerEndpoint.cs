using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.GetSellerEscrowLedger;

namespace OIO.Api.Endpoints.PaymentContext.SellerFinance;

public sealed class GetSellerEscrowLedgerEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.SellerFinance.EscrowLedger, async (
                int? skip,
                int? take,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetSellerEscrowLedgerQuery(skip ?? 0, take ?? 50);
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.SellerFinance.GetSellerEscrowLedger)
            .WithTags(ApiEndpoint.Tags.SellerFinance)
            .Produces<IReadOnlyList<SellerEscrowLedgerRowDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
