using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetWarehouseStaffOutboundQueue;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetWarehouseStaffOutboundQueueEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.WarehouseStaffOutboundQueue, async (
                [AsParameters] PagedParameters parameters,
                ISender sender,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(new GetWarehouseStaffOutboundQueueQuery(parameters), ct);
                return result.ToOkHttpResult();
            })
            // BookOutbound is the warehouse-staff-only permission that already
            // gates the sibling booking endpoint, so the queue that feeds it
            // mirrors the same authorization.
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.BookOutbound)
            .WithName(ApiEndpoint.Names.Warehouse.GetWarehouseStaffOutboundQueue)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<PagedList<WarehouseStaffOutboundQueueItemDto>>(StatusCodes.Status200OK);
    }
}
