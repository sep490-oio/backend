using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetInboundShipmentById;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetInboundShipmentByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.InboundShipmentById, async (
                Guid shipmentId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetInboundShipmentByIdQuery(shipmentId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetInboundShipmentById)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<InboundShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}