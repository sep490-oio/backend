using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Commands.Withdrawals;
using OIO.Application.Context.PaymentContext.DTOs;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class CancelWithdrawalEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.CancelWithdrawal, async (
                Guid withdrawalId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new CancelWithdrawalRequestCommand(withdrawalId),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Wallet.CancelWithdrawal)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<WithdrawalRequestDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
