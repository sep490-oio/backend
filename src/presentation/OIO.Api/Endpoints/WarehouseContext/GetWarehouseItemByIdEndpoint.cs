using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetWarehouseItemById;
using OIO.Application.Extensions;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetWarehouseItemByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.WarehouseItemById, async (
                Guid warehouseItemId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetWarehouseItemByIdQuery(warehouseItemId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetWarehouseItemById)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<WarehouseItemDetailDto>(StatusCodes.Status200OK);
    }
}
