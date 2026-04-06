using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetMyOutboundShipments;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class GetMyOutboundShipmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyOutboundShipments, async (
                [AsParameters] PagedParameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyOutboundShipmentsQuery(parameters), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadOutboundShipments)
            .WithName(ApiEndpoint.Names.Me.GetMyOutboundShipments)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<PagedList<OutboundShipmentDto>>(StatusCodes.Status200OK);
    }
}
