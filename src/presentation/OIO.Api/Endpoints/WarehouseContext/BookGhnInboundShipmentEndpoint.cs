using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.BookInboundShipment;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class BookGhnInboundShipmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        // Custom endpoint for GHN with metadata
        app.MapPost("api/warehouse/ghn/book-inbound", async (
                BookGhnInboundShipmentCommand command,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.BookInbound)
            .WithName("Warehouse.BookGhnInboundShipment")
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<List<InboundShipmentDto>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
