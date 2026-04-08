using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.Commands.UpdateOrderShipping;
using OIO.Application.Context.OrderContext.DTOs;

namespace OIO.Api.Endpoints.OrderContext;

/// <summary>
/// PUT /api/orders/{orderId}/shipping
/// Buyer updates the shipping snapshot on a pending-payment order before
/// running the actual payment flow. Validates the full address-book shape
/// (recipient, phone, street, ward, district, city) server-side.
/// </summary>
public sealed class UpdateOrderShippingEndpoint : IEndpoint
{
    public sealed record Request(
        string RecipientName,
        string PhoneNumber,
        string Street,
        string Ward,
        string District,
        string City,
        string? PostalCode);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Orders.UpdateShipping, async (
                Guid orderId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new UpdateOrderShippingCommand(
                        OrderId: orderId,
                        RecipientName: request.RecipientName,
                        PhoneNumber: request.PhoneNumber,
                        Street: request.Street,
                        Ward: request.Ward,
                        District: request.District,
                        City: request.City,
                        PostalCode: request.PostalCode),
                    ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Orders.UpdateOrderShipping)
            .WithTags(ApiEndpoint.Tags.Orders)
            .Produces<OrderDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
