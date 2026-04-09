using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetBuyerOutboundShipmentByToken;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetBuyerOutboundShipmentByTokenEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.BuyerOutboundShipmentByToken, async (
                string token,
                ISender sender,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(new GetBuyerOutboundShipmentByTokenQuery(token), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Warehouse.GetBuyerOutboundShipmentByToken)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<BuyerOutboundShipmentDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
