using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.Commands.ConfirmWarehouseReturnReceipt;
using OIO.Application.Context.WarehouseContext.Commands.MarkWarehouseReturnDelivered;
using OIO.Application.Context.WarehouseContext.Commands.MarkWarehouseReturnShipped;
using OIO.Application.Context.WarehouseContext.Commands.RecordWarehouseReturnDeliveryFailure;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetMyWarehouseReturns;
using OIO.Application.Context.WarehouseContext.Queries.GetPendingStaffReturns;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext.WarehouseReturns;

/// <summary>
/// POST /api/warehouse-staff/returns/{id}/ship — warehouse staff enter tracking.
/// </summary>
public sealed class MarkWarehouseReturnShippedEndpoint : IEndpoint
{
    public sealed record Request(
        [Required] string ProviderCode,
        [Required] string TrackingNumber,
        DateTime? ShippedAt);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.WarehouseStaffReturnMarkShipped, async (
                Guid id,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new MarkWarehouseReturnShippedCommand(
                        ShipmentId:     id,
                        ProviderCode:   request.ProviderCode,
                        TrackingNumber: request.TrackingNumber,
                        ShippedAt:      request.ShippedAt ?? DateTime.UtcNow),
                    ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.BookOutbound)
            .WithName(ApiEndpoint.Names.Warehouse.MarkWarehouseReturnShipped)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// POST /api/warehouse-staff/returns/{id}/delivery-failure — log a failed delivery.
/// </summary>
public sealed class RecordWarehouseReturnDeliveryFailureEndpoint : IEndpoint
{
    public sealed record Request([Required] string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.WarehouseStaffReturnDeliveryFailure, async (
                Guid id,
                Request request,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new RecordWarehouseReturnDeliveryFailureCommand(id, request.Reason),
                    ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.BookOutbound)
            .WithName(ApiEndpoint.Names.Warehouse.RecordWarehouseReturnDeliveryFailure)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// POST /api/warehouse-staff/returns/{id}/mark-delivered — staff marks manual delivery.
/// </summary>
public sealed class MarkWarehouseReturnDeliveredEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.WarehouseStaffReturnMarkDelivered, async (
                Guid id,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new MarkWarehouseReturnDeliveredCommand(id),
                    ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.BookOutbound)
            .WithName(ApiEndpoint.Names.Warehouse.MarkWarehouseReturnDelivered)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// GET /api/warehouse-staff/returns — warehouse staff pending-returns queue.
/// </summary>
public sealed class GetPendingStaffReturnsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.WarehouseStaffReturns, async (
                [AsParameters] PagedParameters parameters,
                string? status,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetPendingStaffReturnsQuery(parameters, status), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetPendingStaffReturns)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<PagedList<WarehouseToSellerShipmentDto>>(StatusCodes.Status200OK);
    }
}

/// <summary>
/// POST /api/seller/warehouse-returns/{id}/confirm-receipt — seller confirms receipt.
/// </summary>
public sealed class ConfirmWarehouseReturnReceiptEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoint.Url.Warehouse.SellerWarehouseReturnConfirmReceipt, async (
                Guid id,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new ConfirmWarehouseReturnReceiptCommand(id), ct);
                return result.ToNoContentHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.ConfirmWarehouseReturnReceipt)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}

/// <summary>
/// GET /api/seller/warehouse-returns — seller's own warehouse-return inbox.
/// </summary>
public sealed class GetMyWarehouseReturnsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.SellerWarehouseReturns, async (
                [AsParameters] PagedParameters parameters,
                string? status,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetMyWarehouseReturnsQuery(parameters, status), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.ReadShipments)
            .WithName(ApiEndpoint.Names.Warehouse.GetMyWarehouseReturns)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<PagedList<WarehouseToSellerShipmentDto>>(StatusCodes.Status200OK);
    }
}
