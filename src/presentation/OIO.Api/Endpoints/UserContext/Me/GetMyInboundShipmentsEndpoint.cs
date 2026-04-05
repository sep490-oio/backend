using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetMyInboundShipments;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public sealed class GetMyInboundShipmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMyInboundShipments, async (
                [AsParameters] PagedParameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyInboundShipmentsQuery(parameters), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadInboundShipments)
            .WithName(ApiEndpoint.Names.Me.GetMyInboundShipments)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<PagedList<InboundShipmentDto>>(StatusCodes.Status200OK);
    }
}
