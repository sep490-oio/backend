using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Queries.GetSellerOutboundShipments;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

/// <summary>
/// GET /api/me/orders/seller-direct-ship/outbound-shipments
/// Seller-safe paged list of outbound shipments owned by the current seller.
/// Returns the rich SellerOutboundShipmentDto with product + recipient + order context.
/// </summary>
public sealed class GetSellerOutboundShipmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetSellerOutboundShipments, async (
                [AsParameters] PagedParameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSellerOutboundShipmentsQuery(parameters), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadDirectShipOrders)
            .WithName(ApiEndpoint.Names.Me.GetSellerOutboundShipments)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<PagedList<SellerOutboundShipmentDto>>(StatusCodes.Status200OK);
    }
}
