using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.Commands.SellerOrderProgression;
using OIO.Application.Context.OrderContext.DTOs;

namespace OIO.Api.Endpoints.OrderContext;

/// <summary>
/// POST /api/orders/{orderId}/mark-picked-up
/// Seller (self-ship only) advances a Processing order to PickedUp.
/// </summary>
public sealed class MarkOrderPickedUpEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.MarkPickedUp, async (
                Guid orderId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new MarkOrderPickedUpCommand(orderId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.MarkOrderPickedUp)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<OrderDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// POST /api/orders/{orderId}/mark-on-delivering
/// Seller (self-ship only) advances a PickedUp order to OnDelivering.
/// </summary>
public sealed class MarkOrderOnDeliveringEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.MarkOnDelivering, async (
                Guid orderId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new MarkOrderOnDeliveringCommand(orderId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.MarkOrderOnDelivering)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<OrderDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// POST /api/orders/{orderId}/mark-delivered
/// Seller (self-ship only) advances an OnDelivering order to Delivered.
/// </summary>
public sealed class MarkOrderDeliveredEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.MarkDelivered, async (
                Guid orderId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new MarkOrderDeliveredCommand(orderId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.MarkOrderDelivered)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<OrderDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
