using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.Commands.CancelOrderPayment;

namespace OIO.Api.Endpoints.OrderContext;

public sealed class CancelOrderPaymentEndpoint : IEndpoint
{
    public sealed record CancelOrderPaymentRequest(string? Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.CancelPayment, async (
                Guid orderId,
                CancelOrderPaymentRequest? request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new CancelOrderPaymentCommand(orderId, request?.Reason), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.CancelOrderPayment)
            .WithTags(ApiEndpoint.Tags.Orders)
            .WithSummary("Buyer cancels payment for a pending order. Auction-win: 50% deposit penalty + runner-up flow. Buy-now: releases reservation with compensation.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
