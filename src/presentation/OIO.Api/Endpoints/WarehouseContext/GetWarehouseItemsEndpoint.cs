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
                [AsParameters] GetWarehouseItemsQueryFilter parameters,
                ISender sender = default!,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(new GetWarehouseItemsQuery(parameters), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetWarehouseItems)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<PagedList<WarehouseItemDto>>(StatusCodes.Status200OK);

    }
}