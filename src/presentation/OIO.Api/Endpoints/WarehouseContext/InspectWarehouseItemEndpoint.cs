using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.InspectWarehouseItem;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class InspectWarehouseItemEndpoint : IEndpoint
{
    /// <summary>
    /// All 4 photo slots are optional individually.
    /// Upload IDs must be pre-confirmed via the media upload endpoint.
    /// </summary>
    public sealed record Request(
        string? InspectionNotes,
        /// <summary>Upload ID for the front/exterior photo of the package.</summary>
        Guid? FrontPhotoUploadId,
        /// <summary>Upload ID for the shipping label photo.</summary>
        Guid? ShippingLabelUploadId,
        /// <summary>Upload ID for the seal/tape condition photo.</summary>
        Guid? SealConditionUploadId,
        /// <summary>Upload ID for the inside contents photo.</summary>
        Guid? InsideContentsUploadId);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.InspectWarehouseItem, async (
                Guid shipmentId,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var command = new InspectWarehouseItemCommand(
                    shipmentId,
                    request.InspectionNotes,
                    request.FrontPhotoUploadId,
                    request.ShippingLabelUploadId,
                    request.SealConditionUploadId,
                    request.InsideContentsUploadId);

                var result = await sender.Send(command, ct);
                return result.ToCreatedHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.Inspect)
            .WithName(ApiEndpoint.Names.Warehouse.InspectWarehouseItem)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<WarehouseItemDto>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}