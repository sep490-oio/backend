using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Queries.GetShipmentByScanCode;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class ScanShipmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/warehouse/inbound-shipments/scan", async (
            [FromQuery] string code,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var command = new GetShipmentByScanCodeQuery(code);

            var result = await sender.Send(command, cancellationToken);

            return result.IsFailure
                ? Results.BadRequest(result.Error)
                : Results.Ok(result.Value);
        })
        .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
        .WithTags(ApiEndpoint.Tags.Warehouse)
        .WithName("Scan Inbound Shipment")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound);
    }
}
