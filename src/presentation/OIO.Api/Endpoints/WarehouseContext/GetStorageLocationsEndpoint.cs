using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetStorageLocations;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetStorageLocationsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.StorageLocations, async (
                bool?   vacantOnly,
                string? zone,
                string? search,
                int     page = 1,
                int     pageSize = 50,
                ISender sender = default!,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(
                    new GetStorageLocationsQuery(
                        vacantOnly ?? false, zone, search, page, pageSize), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ManageLocations)
            .WithName(ApiEndpoint.Names.Warehouse.GetStorageLocations)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<IReadOnlyList<StorageLocationDto>>(StatusCodes.Status200OK);
    }
}