using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.GetMyWallet;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class GetMyWalletEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyWallet, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyWalletQuery(), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Wallet.GetMyWallet)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<WalletSummaryDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
