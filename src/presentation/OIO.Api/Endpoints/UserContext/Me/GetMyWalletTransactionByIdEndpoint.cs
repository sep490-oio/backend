using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.GetMyWalletTransactionById;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class GetMyWalletTransactionByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyWalletTransactionById, async (
                Guid transactionId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetMyWalletTransactionByIdQuery(transactionId),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Wallet.GetMyWalletTransactionById)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<WalletTransactionDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
