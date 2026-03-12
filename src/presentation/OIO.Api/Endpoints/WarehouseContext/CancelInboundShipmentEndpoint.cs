using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.CancelInboundShipment;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class CancelInboundShipmentEndpoint : IEndpoint
{
    public sealed record Request(string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.CancelInbound, async (
                Guid shipmentId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new CancelInboundShipmentCommand(shipmentId, request.Reason), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.CancelInboundShipment)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}