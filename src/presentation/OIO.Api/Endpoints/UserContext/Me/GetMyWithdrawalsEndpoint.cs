using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.PaymentContext.DTOs;
using OIO.Application.Context.PaymentContext.Queries.GetMyWithdrawals;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class GetMyWithdrawalsEndpoint : IEndpoint
{
    public sealed record Parameters : WithdrawalFilterParameters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyWithdrawals, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetMyWithdrawalsQuery(parameters),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Wallet.GetMyWithdrawals)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<PagedList<WithdrawalRequestDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
