using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetSellerWarehouseItems;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetSellerWarehouseItemsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.SellerWarehouseItems, async (
                int?              pageNumber,
                int?              pageSize,
                string?           warehouseFlowStatus,
                string?           search,
                ISender           sender,
                CancellationToken ct) =>
            {
                var filter = new GetSellerWarehouseItemsQueryFilter
                {
                    PageNumber          = pageNumber,
                    PageSize            = pageSize,
                    WarehouseFlowStatus = warehouseFlowStatus,
                    Search              = search,
                };
                var result = await sender.Send(new GetSellerWarehouseItemsQuery(filter), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetSellerWarehouseItems)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<PagedList<SellerWarehouseItemListItemDto>>(StatusCodes.Status200OK);
    }
}
