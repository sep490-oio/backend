using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.DTOs;

using OIO.Application.Context.WarehouseContext.Queries.GetOutboundShipments;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetOutboundShipmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.BookOutbound, async (
                string?   status,
                Guid?     orderId,
                string?   search,
                DateTime? fromDate,
                DateTime? toDate,
                int       page = 1,
                int       pageSize = 20,
                ISender sender = default!,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(
                    new GetOutboundShipmentsQuery(
                        status, orderId, search, fromDate, toDate, page, pageSize), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetOutboundShipments)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<PagedList<OutboundShipmentDto>>(StatusCodes.Status200OK);

    }
}