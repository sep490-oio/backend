using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.Commands.ConfirmOrderReturnReceived;
using OIO.Application.Context.OrderContext.DTOs;

namespace OIO.Api.Endpoints.OrderContext;

public sealed class ConfirmOrderReturnReceivedEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.ConfirmReturnReceived, async (
                Guid orderId,
                Guid returnId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new ConfirmOrderReturnReceivedCommand(orderId, returnId),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.ConfirmOrderReturnReceived)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<OrderReturnDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
