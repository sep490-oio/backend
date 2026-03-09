using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.StoreWarehouseItem;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class StoreWarehouseItemEndpoint : IEndpoint
{
    public sealed record Request(Guid StorageLocationId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.StoreItem, async (
                Guid warehouseItemId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new StoreWarehouseItemCommand(
                    warehouseItemId,
                    request.StorageLocationId);

                var result = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.Store)
            .WithName(ApiEndpoint.Names.Warehouse.StoreWarehouseItem)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<WarehouseItemDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}