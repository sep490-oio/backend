using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.Commands.ShipOrderReturn;
using OIO.Application.Context.OrderContext.DTOs;

namespace OIO.Api.Endpoints.OrderContext;

public sealed class ShipOrderReturnEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string ProviderCode,
        [Required] string TrackingNumber);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.ShipReturn, async (
                Guid orderId,
                Guid returnId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new ShipOrderReturnCommand(orderId, returnId, request.ProviderCode, request.TrackingNumber),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.ShipOrderReturn)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<OrderReturnDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
