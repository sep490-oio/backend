using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.OrderContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.PaymentContext.Aggregates.Escrows;
using OIO.Domain.Context.PaymentContext.Enums;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

/// <summary>
/// Bug #12 fix: when ANY caller invokes Auction.Terminate (not just the emergency command),
/// this handler runs the cross-aggregate cleanup that was previously inline in
/// TriggerAuctionEmergencyCommand: cancel PendingPayment orders, refund Holding escrow,
/// cancel pending/booked outbound shipments. Idempotent — re-running on already-cleaned
/// state is a no-op.
///
/// Seller-suspend / risk-flag side effects remain in TriggerAuctionEmergencyCommand
/// since those are specific to admin emergency action, not all terminations.
/// </summary>
internal sealed class AuctionTerminatedCleanupHandler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IClock clock,
    EscrowSettlementService settlementService,
    ILogger<AuctionTerminatedCleanupHandler> logger)
    : INotificationHandler<AuctionTerminatedEvent>
{
    public async Task Handle(AuctionTerminatedEvent notification, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(notification.AuctionId, out var auctionGuid))
            return;

        var auctionId = AuctionId.From(auctionGuid);
        var nowUtc = clock.UtcNow;

        // 1) Cancel any PendingPayment orders for this auction.
        var orders = await dbContext.Set<Order>()
            .Include(o => o.Escrows)
            .Where(o => o.AuctionId == auctionId &&
                        (o.Status == OrderStatus.PendingPayment || o.Escrows.Any(e => e.Status == EscrowStatus.Holding)))
            .ToListAsync(cancellationToken);

        foreach (var order in orders)
        {
            if (order.Status == OrderStatus.PendingPayment)
            {
                var cancelResult = order.Cancel(
                    $"Auction terminated: {notification.Reason}",
                    nowUtc);
                if (cancelResult.IsFailure)
                {
                    logger.LogWarning(
                        "AuctionTerminatedCleanup: failed to cancel order {OrderId}: {Error}",
                        order.Id.Value, cancelResult.Error.Message);
                }
            }
            else if (order.Escrows.Any(e => e.Status == EscrowStatus.Holding))
            {
                var refundResult = await settlementService.RefundBuyerAsync(
                    order,
                    partialAmount: null,
                    reason: $"Auction terminated: {notification.Reason}",
                    actorId: null,
                    cancellationToken: cancellationToken);
                if (refundResult.IsFailure)
                {
                    logger.LogWarning(
                        "AuctionTerminatedCleanup: failed to refund escrow for order {OrderId}: {Error}",
                        order.Id.Value, refundResult.Error.Message);
                }
            }
        }

        // 2) Cancel pending/booked outbound shipments tied to this auction's orders.
        if (orders.Count > 0)
        {
            var orderIds = orders.Select(o => o.Id).ToList();
            var shipments = await dbContext.Set<OutboundShipment>()
                .Where(s => s.OrderId != null && orderIds.Contains(s.OrderId) &&
                            (s.Status == OutboundShipmentStatus.Pending ||
                             s.Status == OutboundShipmentStatus.Booked))
                .ToListAsync(cancellationToken);

            foreach (var shipment in shipments)
            {
                var cancelResult = shipment.Cancel(
                    $"Auction terminated: {notification.Reason}",
                    nowUtc);
                if (cancelResult.IsFailure)
                {
                    logger.LogWarning(
                        "AuctionTerminatedCleanup: failed to cancel shipment {ShipmentId}: {Error}",
                        shipment.Id.Value, cancelResult.Error.Message);
                }
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
