using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.BookInboundShipment;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class BookInboundShipmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.BookInbound, async (
                BookInboundShipmentCommand command,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.BookInbound)
            .WithName(ApiEndpoint.Names.Warehouse.BookInboundShipment)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<List<InboundShipmentDto>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}