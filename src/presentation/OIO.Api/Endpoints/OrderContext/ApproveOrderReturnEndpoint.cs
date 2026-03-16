using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.Commands.ApproveOrderReturn;
using OIO.Application.Context.OrderContext.DTOs;

namespace OIO.Api.Endpoints.OrderContext;

public sealed class ApproveOrderReturnEndpoint : IEndpoint
{
    public sealed record Request(string? Notes);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.ApproveReturn, async (
                Guid orderId,
                Guid returnId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new ApproveOrderReturnCommand(orderId, returnId, request.Notes),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.ApproveOrderReturn)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<OrderReturnDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
