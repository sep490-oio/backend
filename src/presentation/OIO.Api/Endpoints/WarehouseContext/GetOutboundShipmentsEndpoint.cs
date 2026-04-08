using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.DTOs;

using OIO.Application.Context.WarehouseContext.Queries.GetOutboundShipments;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetOutboundShipmentsEndpoint : IEndpoint
{
    public sealed record Parameters : GetOutboundShipmentsQueryFilters;
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.BookOutbound, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(
                    new GetOutboundShipmentsQuery(
                        parameters), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetOutboundShipments)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<PagedList<OutboundShipmentDto>>(StatusCodes.Status200OK);

    }
}