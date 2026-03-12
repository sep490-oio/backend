using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.DeleteStorageLocation;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class DeleteStorageLocationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete(ApiEndpoint.Url.Warehouse.StorageLocationById, async (
                Guid locationId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new DeleteStorageLocationCommand(locationId), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ManageLocations)
            .WithName(ApiEndpoint.Names.Warehouse.DeleteStorageLocation)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}