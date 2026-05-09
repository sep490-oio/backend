using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.Services;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Queries.GetBuyerOutboundShipmentByToken;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Commands.AcknowledgeOutboundShipmentReceived;

public sealed record AcknowledgeOutboundShipmentReceivedCommand(
    Guid ShipmentId,
    string? Source) : ICommand<BuyerOutboundShipmentDetailDto>;

internal sealed class AcknowledgeOutboundShipmentReceivedCommandHandler(
    IDbContext db,
    IUnitOfWork unitOfWork,
    IClock clock,
    ICurrentUser currentUser,
    IOrderDeliveryService orderDeliveryService)
    : ICommandHandler<AcknowledgeOutboundShipmentReceivedCommand, BuyerOutboundShipmentDetailDto>
{
    private static readonly HashSet<string> AllowedSources =
        new(StringComparer.Ordinal) { "qr_scan", "order_page", "receipt_proof" };

    public async Task<Result<BuyerOutboundShipmentDetailDto, Error>> Handle(
        AcknowledgeOutboundShipmentReceivedCommand request,
        CancellationToken cancellationToken)
    {
        var source = string.IsNullOrWhiteSpace(request.Source) ? "qr_scan" : request.Source!;
        if (!AllowedSources.Contains(source))
            return Error.Validation(
                "source",
                "OutboundShipment.InvalidAcknowledgeSource",
                $"Source must be one of: {string.Join(", ", AllowedSources)}.");

        var shipmentId = OutboundShipmentId.From(request.ShipmentId);
        var shipment = await db.Set<OutboundShipment>()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken);

        if (shipment is null)
            return Error.NotFound("OutboundShipment.NotFound", "Shipment was not found.");

        var order = await db.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == shipment.OrderId, cancellationToken);

        // Don't leak existence on ownership mismatch — mirrors the pattern
        // used by SellerDirectShipmentLoader.
        if (order is null || order.BuyerId != currentUser.UserId)
            return Error.NotFound("OutboundShipment.NotFound", "Shipment was not found.");

        var now = clock.UtcNow;

        var ackResult = shipment.AcknowledgeReceivedByBuyer(now, source);
        if (ackResult.IsFailure)
            return ackResult.Error;

        // ── Auto-deliver: when the buyer physically confirms receipt but the
        // shipment/order haven't been marked as delivered by warehouse staff
        // yet, promote both to Delivered so the buyer can immediately proceed
        // to inspect-and-accept without waiting for a staff action.
        if (shipment.Status != OutboundShipmentStatus.Delivered)
        {
            var deliverResult = shipment.RecordDelivered(deliveredAt: now, now: now);
            // Idempotent — ignore failure if already delivered
            if (deliverResult.IsFailure &&
                shipment.Status != OutboundShipmentStatus.Delivered)
                return deliverResult.Error;
        }

        var orderDeliveredResult = await orderDeliveryService.MarkAsDeliveredAsync(order, now, cancellationToken);
        if (orderDeliveredResult.IsFailure) return orderDeliveredResult.Error;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuyerOutboundShipmentDetailBuilder.BuildAsync(db, shipment, order, cancellationToken);
    }
}
