using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.DTOs;

using OIO.Application.Context.WarehouseContext.Queries.GetInboundShipments;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetInboundShipmentsEndpoint : IEndpoint
{
    public sealed record Parameters : GetInboundShipmentsQueryFilter;
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.BookInbound, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(
                    new GetInboundShipmentsQuery(
                        parameters), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetInboundShipments)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<PagedList<InboundShipmentDto>>(StatusCodes.Status200OK);

    }
}