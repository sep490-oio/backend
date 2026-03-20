using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.UpdateExternalShipmentStatus;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class UpdateExternalShipmentStatusEndpoint : IEndpoint
{
    public sealed record Request(string Status);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(ApiEndpoint.Url.Warehouse.UpdateExternalStatus, async (
                Guid shipmentId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new UpdateExternalShipmentStatusCommand(shipmentId, request.Status);
                var result  = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.UpdateExternalStatus)
            .WithName(ApiEndpoint.Names.Warehouse.UpdateExternalShipmentStatus)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<InboundShipmentDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}