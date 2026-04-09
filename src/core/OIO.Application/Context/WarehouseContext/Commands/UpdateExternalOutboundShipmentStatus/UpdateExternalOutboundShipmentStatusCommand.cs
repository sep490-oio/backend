using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetWarehouseStaffOutboundShipmentById;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.Errors;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.UpdateExternalOutboundShipmentStatus;

/// <summary>
/// Manual status transition applied by warehouse staff to an external-carrier
/// outbound shipment. Platform-managed shipments are driven by carrier webhooks
/// only and cannot be updated through this command.
/// </summary>
public sealed record UpdateExternalOutboundShipmentStatusCommand(
    Guid ShipmentId,
    string Status) : ICommand<WarehouseStaffOutboundShipmentDetailDto>;

internal sealed class UpdateExternalOutboundShipmentStatusCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    IOrderDeliveryService orderDeliveryService,
    IClock clock)
    : ICommandHandler<UpdateExternalOutboundShipmentStatusCommand, WarehouseStaffOutboundShipmentDetailDto>
{
    private static readonly string[] AllowedTargets = ["picked_up", "delivering", "delivered", "failed", "returning", "returned"];

    public async Task<Result<WarehouseStaffOutboundShipmentDetailDto, Error>> Handle(
        UpdateExternalOutboundShipmentStatusCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Status) || !AllowedTargets.Contains(request.Status))
        {
            return Error.Validation(
                "status",
                "OutboundShipment.InvalidManualStatus",
                $"Status must be one of: {string.Join(", ", AllowedTargets)}.");
        }

        var shipmentId = OutboundShipmentId.From(request.ShipmentId);
        var shipment = await db.Set<OutboundShipment>()
            .Include(s => s.TrackingEvents)
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return WarehouseErrors.OutboundShipment.NotFound(request.ShipmentId.ToString());

        if (shipment.ShipmentMode != OutboundShipmentMode.ExternalCarrier)
        {
            return Error.Conflict(
                "OutboundShipment.NotExternalCarrier",
                "Only external-carrier shipments can be updated manually.");
        }

        var now = clock.UtcNow;

        var result = request.Status switch
        {
            "picked_up" => shipment.RecordPickedUp(now),
            "delivering" => shipment.MarkDelivering(now),
            "delivered" => shipment.RecordDelivered(now, now),
            "failed"    => shipment.RecordFailed("Marked failed by warehouse staff", now),
            "returning" => shipment.RecordReturning("Marked returning by warehouse staff", now),
            "returned"  => shipment.RecordReturned(now),
            _           => UnitResult.Success<Error>()
        };

        if (result.IsFailure) return result.Error;

        if (request.Status == "delivered")
        {
            var order = await db.Set<Order>()
                .FirstOrDefaultAsync(o => o.Id == shipment.OrderId, cancellationToken);
            if (order is not null)
            {
                var orderDeliveredResult = await orderDeliveryService.MarkAsDeliveredAsync(order, now, cancellationToken);
                if (orderDeliveredResult.IsFailure) return orderDeliveredResult.Error;
            }
        }

        db.Update(shipment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await StaffOutboundShipmentDetailBuilder.BuildAsync(db, shipment, cancellationToken);
    }
}
