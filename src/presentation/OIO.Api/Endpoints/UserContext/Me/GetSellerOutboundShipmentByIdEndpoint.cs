using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Queries.GetSellerOutboundShipmentById;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

/// <summary>
/// GET /api/me/orders/seller-direct-ship/outbound-shipments/{shipmentId}
/// Seller-safe read path for a single outbound shipment. Authorization is
/// enforced in the handler (order.SellerId == currentUser).
/// </summary>
public sealed class GetSellerOutboundShipmentByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetSellerOutboundShipmentById, async (
                Guid shipmentId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSellerOutboundShipmentByIdQuery(shipmentId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadDirectShipOrders)
            .WithName(ApiEndpoint.Names.Me.GetSellerOutboundShipmentById)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<SellerOutboundShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
