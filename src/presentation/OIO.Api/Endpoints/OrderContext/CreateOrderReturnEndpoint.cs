using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.Commands.RequestOrderReturn;
using OIO.Application.Context.OrderContext.DTOs;

namespace OIO.Api.Endpoints.OrderContext;

public sealed class CreateOrderReturnEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string ReasonCode,
        string? Description = null);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.CreateReturn, async (
                Guid orderId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new RequestOrderReturnCommand(orderId, request.ReasonCode, request.Description),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.CreateOrderReturn)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<OrderReturnDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
