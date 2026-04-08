using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.Commands.ConfirmSellerOrder;
using OIO.Application.Context.OrderContext.DTOs;

namespace OIO.Api.Endpoints.OrderContext;

/// <summary>
/// POST /api/orders/{orderId}/confirm
/// Seller confirms a paid order and begins fulfillment (Paid → Processing).
/// No body required — orderId is in the route and the caller's identity is
/// resolved from the auth context.
/// </summary>
public sealed class ConfirmSellerOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.Confirm, async (
                Guid orderId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new ConfirmSellerOrderCommand(orderId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.ConfirmSellerOrder)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<OrderDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
