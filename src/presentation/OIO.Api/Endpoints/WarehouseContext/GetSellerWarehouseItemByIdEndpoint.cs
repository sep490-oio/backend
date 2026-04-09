using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetSellerWarehouseItemById;
using OIO.Application.Extensions;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetSellerWarehouseItemByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.SellerWarehouseItemById, async (
                Guid warehouseItemId,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetSellerWarehouseItemByIdQuery(warehouseItemId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetSellerWarehouseItemById)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<SellerWarehouseItemDetailDto>(StatusCodes.Status200OK);
    }
}
