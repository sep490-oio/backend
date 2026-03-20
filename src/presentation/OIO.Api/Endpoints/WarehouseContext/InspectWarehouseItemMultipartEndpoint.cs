using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.InspectWarehouseItemMultipart;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

/// <summary>
/// Single-step inspection endpoint for warehouse staff on mobile.
/// Accepts raw photo files via multipart/form-data — no pre-upload or confirm step required.
/// The server uploads photos to Cloudinary internally.
///
/// POST /api/warehouse/inbound-shipments/{shipmentId}/inspect/multipart
/// Content-Type: multipart/form-data
///   inspectionNotes      (optional string)
///   frontPhoto           (optional image file)
///   shippingLabelPhoto   (optional image file)
///   sealConditionPhoto   (optional image file)
///   insideContentsPhoto  (optional image file)
/// </summary>
public sealed class InspectWarehouseItemMultipartEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.InspectMultipart, async (
                HttpContext       httpContext,
                Guid              shipmentId,
                ISender           sender,
                CancellationToken ct,
                [FromForm] string? inspectionNotes     = null,
                IFormFile?        frontPhoto           = null,
                IFormFile?        shippingLabelPhoto   = null,
                IFormFile?        sealConditionPhoto   = null,
                IFormFile?        insideContentsPhoto  = null) =>
            {
                var command = new InspectWarehouseItemMultipartCommand(
                    InboundShipmentId:  shipmentId,
                    InspectionNotes:    inspectionNotes,
                    FrontPhoto:         frontPhoto,
                    ShippingLabelPhoto: shippingLabelPhoto,
                    SealConditionPhoto: sealConditionPhoto,
                    InsideContentsPhoto: insideContentsPhoto);

                var result = await sender.Send(command, ct);
                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.Inspect)
            .WithName(ApiEndpoint.Names.Warehouse.InspectWarehouseItemMultipart)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .DisableAntiforgery()                          // required for multipart in minimal APIs
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<WarehouseItemDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}