using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.UpdateStorageLocation;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class UpdateStorageLocationEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string Zone, 
        [Required] string Aisle,
        [Required] string Shelf,
        [Required] string Bin);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoint.Url.Warehouse.StorageLocationById, async (
                Guid locationId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new UpdateStorageLocationCommand(
                        locationId,
                        request.Zone,
                        request.Aisle,
                        request.Shelf,
                        request.Bin), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ManageLocations)
            .WithName(ApiEndpoint.Names.Warehouse.UpdateStorageLocation)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<StorageLocationDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}