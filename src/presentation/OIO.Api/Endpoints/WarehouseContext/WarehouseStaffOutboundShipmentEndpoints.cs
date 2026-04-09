using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.WarehouseContext.Commands.UpdateExternalOutboundShipmentStatus;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetWarehouseStaffOutboundShipmentById;
using OIO.Application.Context.WarehouseContext.Queries.GetWarehouseStaffOutboundShipments;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.WarehouseContext;

public sealed class GetWarehouseStaffOutboundShipmentsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.WarehouseStaffOutboundShipments, async (
                [AsParameters] PagedParameters parameters,
                string? status,
                string? shipmentMode,
                string? search,
                ISender sender,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(
                    new GetWarehouseStaffOutboundShipmentsQuery(parameters, status, shipmentMode, search), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.BookOutbound)
            .WithName(ApiEndpoint.Names.Warehouse.GetWarehouseStaffOutboundShipments)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<PagedList<WarehouseStaffOutboundShipmentListItemDto>>(StatusCodes.Status200OK);
    }
}

public sealed class GetWarehouseStaffOutboundShipmentByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Warehouse.WarehouseStaffOutboundShipmentById, async (
                Guid shipmentId,
                ISender sender,
                CancellationToken ct = default) =>
            {
                var result = await sender.Send(new GetWarehouseStaffOutboundShipmentByIdQuery(shipmentId), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.BookOutbound)
            .WithName(ApiEndpoint.Names.Warehouse.GetWarehouseStaffOutboundShipmentById)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<WarehouseStaffOutboundShipmentDetailDto>(StatusCodes.Status200OK);
    }
}

public sealed class UpdateExternalOutboundShipmentStatusEndpoint : IEndpoint
{
    public record Body(string Status);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch(ApiEndpoint.Url.Warehouse.WarehouseStaffOutboundShipmentStatus, async (
                Guid shipmentId,
                Body body,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(
                    new UpdateExternalOutboundShipmentStatusCommand(shipmentId, body.Status), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Warehouse.BookOutbound)
            .WithName(ApiEndpoint.Names.Warehouse.UpdateExternalOutboundShipmentStatus)
            .WithTags(ApiEndpoint.Tags.Warehouse)
            .Produces<WarehouseStaffOutboundShipmentDetailDto>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
