using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.SubmitOutboundShipmentReceiptProof;
using OIO.Application.Context.WarehouseContext.DTOs;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class SubmitOutboundShipmentReceiptProofEndpoint : IEndpoint
{
    public sealed record Body(List<Guid> ReceiptPhotoMediaUploadIds);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Me.SubmitOutboundShipmentReceiptProof, async (
                Guid shipmentId,
                Body body,
                ISender sender,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(
                    new SubmitOutboundShipmentReceiptProofCommand(shipmentId, body.ReceiptPhotoMediaUploadIds),
                    ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.SubmitOutboundShipmentReceiptProof)
            .WithTags(ApiEndpoint.Tags.Me)
            .Produces<BuyerOutboundShipmentDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
