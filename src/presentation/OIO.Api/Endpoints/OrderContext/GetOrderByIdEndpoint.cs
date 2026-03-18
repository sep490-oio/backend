using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Queries.GetOrderById;

namespace OIO.Api.Endpoints.OrderContext;

public sealed class GetOrderByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Orders.GetById, async (
                Guid orderId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetOrderByIdQuery(orderId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.GetOrderById)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<OrderDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
