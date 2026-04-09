using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetBuyerOutboundShipmentById;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetBuyerOutboundShipmentByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetBuyerOutboundShipmentById, async (
                Guid shipmentId,
                ISender sender,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(
                    new GetBuyerOutboundShipmentByIdQuery(shipmentId),
                    ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.GetBuyerOutboundShipmentById)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<BuyerOutboundShipmentDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
