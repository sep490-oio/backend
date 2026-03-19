using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.Commands.RejectOrderReturn;
using OIO.Application.Context.OrderContext.DTOs;

namespace OIO.Api.Endpoints.OrderContext;

public sealed class RejectOrderReturnEndpoint : IEndpoint
{
    public sealed record Request([Required] string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.RejectReturn, async (
                Guid orderId,
                Guid returnId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new RejectOrderReturnCommand(orderId, returnId, request.Reason),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.RejectOrderReturn)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<OrderReturnDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
