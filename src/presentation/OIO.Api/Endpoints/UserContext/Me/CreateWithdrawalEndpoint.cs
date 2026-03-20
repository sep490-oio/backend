using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.PaymentContext.Commands.Withdrawals;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class CreateWithdrawalEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] decimal Amount,
        [Required] string BankName,
        [Required] string AccountNumber,
        [Required] string AccountHolder);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.CreateWithdrawal, async (
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new CreateWithdrawalRequestCommand(
                        request.Amount,
                        request.BankName,
                        request.AccountNumber,
                        request.AccountHolder),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Wallet.CreateWithdrawal)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<CreateWithdrawalRequestResponse>(StatusCodes.Status200OK)
            .Produces<Error>(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
