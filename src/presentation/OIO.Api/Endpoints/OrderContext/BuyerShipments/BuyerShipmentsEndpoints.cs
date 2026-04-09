using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Queries.BuyerShipments;

namespace OIO.Api.Endpoints.OrderContext.BuyerShipments;

/// <summary>
/// GET /api/me/shipments — unified buyer shipment feed. Merges the caller's
/// <c>SellerDirectShipment</c> rows and warehouse-booked <c>OutboundShipment</c>
/// rows into a single paged list discriminated by <c>shipmentKind</c>.
/// </summary>
public sealed class GetMyShipmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyShipments, async (
                [AsParameters] PagedParameters parameters,
                string? status,
                string? shipmentKind,
                string? search,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new GetMyShipmentsQuery(parameters, status, shipmentKind, search), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.GetMyShipments)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<PagedList<BuyerShipmentListItemDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
