using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.CreateStorageLocation;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class CreateStorageLocationEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.StorageLocations, async (
                CreateStorageLocationCommand command,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ManageLocations)
            .WithName(ApiEndpoint.Names.Warehouse.CreateStorageLocation)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<StorageLocationDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}