using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.BookDirectShipment;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class BookDirectShipmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.BookDirectGhn, async (
                BookDirectShipmentCommand command,
                ISender sender,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(command, ct);
                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.SelfShipOutbound)
            .WithName(ApiEndpoint.Names.Warehouse.BookDirectShipment)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<OutboundShipmentDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}
