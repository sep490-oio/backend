using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetOutboundShipmentById;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetOutboundShipmentByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.OutboundShipmentById, async (
                Guid shipmentId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetOutboundShipmentByIdQuery(shipmentId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetOutboundShipmentById)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<OutboundShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}