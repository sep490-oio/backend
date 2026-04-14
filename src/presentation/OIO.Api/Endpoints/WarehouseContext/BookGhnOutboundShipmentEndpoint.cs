using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.BookOutboundShipment;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class BookGhnOutboundShipmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Custom endpoint for GHN with metadata
        app.MapPost("api/warehouse/ghn/book-outbound", async (
                BookGhnOutboundShipmentCommand command,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.BookOutbound)
            .WithName("Warehouse.BookGhnOutboundShipment")
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<OutboundShipmentDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
