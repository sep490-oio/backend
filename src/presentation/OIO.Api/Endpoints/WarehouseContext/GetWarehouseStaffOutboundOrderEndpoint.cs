using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetWarehouseStaffOutboundOrder;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetWarehouseStaffOutboundOrderEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.WarehouseStaffOutboundOrderById, async (
                Guid orderId,
                ISender sender,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(new GetWarehouseStaffOutboundOrderQuery(orderId), ct);
                return result.ToOkHttpResult();
            })
            // Same authorization as the queue / booking endpoints.
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.BookOutbound)
            .WithName(ApiEndpoint.Names.Warehouse.GetWarehouseStaffOutboundOrder)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<WarehouseStaffOutboundQueueItemDto>(StatusCodes.Status200OK);
    }
}
