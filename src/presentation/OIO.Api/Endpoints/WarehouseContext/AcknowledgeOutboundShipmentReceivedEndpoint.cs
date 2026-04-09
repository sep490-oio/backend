using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.AcknowledgeOutboundShipmentReceived;
using OIO.Application.Context.WarehouseContext.DTOs;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class AcknowledgeOutboundShipmentReceivedEndpoint : IEndpoint
{
    public sealed record Body(string? Source);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.AcknowledgeOutboundShipmentReceived, async (
                Guid shipmentId,
                Body? body,
                ISender sender,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(
                    new AcknowledgeOutboundShipmentReceivedCommand(shipmentId, body?.Source),
                    ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.AcknowledgeOutboundShipmentReceived)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<BuyerOutboundShipmentDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
