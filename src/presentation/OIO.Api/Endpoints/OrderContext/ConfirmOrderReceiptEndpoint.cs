using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.Commands.ConfirmOrderReceipt;

namespace OIO.Api.Endpoints.OrderContext;

public sealed class ConfirmOrderReceiptEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Orders.ConfirmReceipt, async (
                Guid orderId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new ConfirmOrderReceiptCommand(orderId), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.ConfirmOrderReceipt)
            .WithTags(ApiEndpoint.Tags.Orders)
            .WithSummary("Buyer confirms receipt of the order, triggering escrow release to the seller.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
