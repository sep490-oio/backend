using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.DTOs;

using OIO.Application.Context.WarehouseContext.Queries.GetWarehouseItems;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetWarehouseItemsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.WarehouseItems, async (
                string? status,
                Guid?   storageLocationId,
                Guid?   itemId,
                Guid?   inboundShipmentId,
                int     page = 1,
                int     pageSize = 20,
                ISender sender = default!,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(
                    new GetWarehouseItemsQuery(
                        status, storageLocationId, itemId, inboundShipmentId, page, pageSize), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetWarehouseItems)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<PagedList<WarehouseItemDto>>(StatusCodes.Status200OK);

    }
}