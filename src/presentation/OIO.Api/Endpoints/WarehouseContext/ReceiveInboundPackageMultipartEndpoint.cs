using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.ReceiveInboundPackageMultipart;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

/// <summary>
/// Package-level receiving. One multipart request receives the entire batch of
/// sibling InboundShipment rows sharing the same ClientOrderCode.
/// </summary>
public sealed class ReceiveInboundPackageMultipartEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.ReceiveInboundPackageMultipart, async (
                HttpContext       httpContext,
                string            clientOrderCode,
                ISender           sender,
                CancellationToken ct,
                [FromForm] string? notes              = null,
                IFormFile?        frontPhoto          = null,
                IFormFile?        shippingLabelPhoto  = null,
                IFormFile?        sealConditionPhoto  = null,
                IFormFile?        insideContentsPhoto = null) =>
            {
                var command = new ReceiveInboundPackageMultipartCommand(
                    ClientOrderCode:     clientOrderCode,
                    Notes:               notes,
                    FrontPhoto:          frontPhoto,
                    ShippingLabelPhoto:  shippingLabelPhoto,
                    SealConditionPhoto:  sealConditionPhoto,
                    InsideContentsPhoto: insideContentsPhoto);

                var result = await sender.Send(command, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.Inspect)
            .WithName(ApiEndpoint.Names.Warehouse.ReceiveInboundPackageMultipart)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<InboundPackageDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
