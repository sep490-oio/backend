using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.BookOutboundShipment;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class BookOutboundShipmentEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.BookOutbound, async (
                BookOutboundShipmentCommand command,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.BookOutbound)
            .WithName(ApiEndpoint.Names.Warehouse.BookOutboundShipment)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }
}