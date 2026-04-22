using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.WarehouseContext.Commands.AddWarehouseReturnEvidence;
using OIO.Application.Context.WarehouseContext.Commands.ScanWarehouseReturn;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext.WarehouseReturns;

/// <summary>
/// POST /api/warehouse-staff/returns/{id}/evidence — warehouse staff uploads
/// a pickup photo. Requires the Warehouse.BookOutbound permission (same as
/// MarkShipped) so the staff-role boundary is the same.
/// </summary>
public sealed class AddWarehouseStaffReturnEvidenceEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] Guid MediaUploadId,
        [Required] string Category);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.WarehouseStaffReturnEvidence, async (
                Guid id,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AddWarehouseReturnEvidenceCommand(id, request.MediaUploadId, request.Category),
                    ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.BookOutbound)
            .WithName(ApiEndpoint.Names.Warehouse.AddWarehouseStaffReturnEvidence)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<WarehouseToSellerShipmentEvidenceDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// POST /api/seller/warehouse-returns/{id}/evidence — seller uploads a
/// receipt photo. Auth is seller-scoped — handler asserts caller ==
/// shipment.SellerId when category is <c>receipt_by_seller</c>.
/// </summary>
public sealed class AddSellerWarehouseReturnEvidenceEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] Guid MediaUploadId,
        [Required] string Category);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.SellerWarehouseReturnEvidence, async (
                Guid id,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new AddWarehouseReturnEvidenceCommand(id, request.MediaUploadId, request.Category),
                    ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.AddSellerWarehouseReturnEvidence)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<WarehouseToSellerShipmentEvidenceDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// POST /api/seller/warehouse-returns/{id}/scan — seller scans the
/// warehouse-staff-issued QR on parcel arrival. Flips the shipment from
/// InTransit → Delivered (parallel to OrderReturn.MarkSellerReceived).
/// </summary>
public sealed class ScanWarehouseReturnEndpoint : IEndpoint
{
    public sealed record Request([Required] string QrToken);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.SellerWarehouseReturnScan, async (
                Guid id,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new ScanWarehouseReturnCommand(id, request.QrToken),
                    ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.ScanWarehouseReturn)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
