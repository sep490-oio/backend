using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.WarehouseContext.EventHandlers;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;

namespace OIO.Application.Context.OrderContext.EventHandlers;

/// <summary>
/// When an Order transitions to Paid, re-derive the fulfillment flow using the
/// same WarehouseItem lookup as <c>OrderMappings.BuildSellerFulfillment</c>.
/// For <c>seller_self_ship</c> orders, stamp the ship-by SLA deadline
/// (<c>PaidAt + IRuntimeSettings.Order.SellerShipSlaDays</c>). Warehouse-managed
/// orders are ignored — the warehouse context handles them separately.
/// </summary>
internal sealed class SellerSelfShipSlaScheduler(
    IDbContext dbContext,
    IUnitOfWork unitOfWork,
    IRuntimeSettings runtimeSettings,
    ILogger<SellerSelfShipSlaScheduler> logger)
    : INotificationHandler<OrderPaidIntegrationEvent>
{
    public async Task Handle(OrderPaidIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var orderId = OrderId.From(notification.OrderId);

        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning(
                "SellerSelfShipSlaScheduler: Order {OrderId} not found; skipping SLA stamp.",
                notification.OrderId);
            return;
        }

        if (order.PaidAt is null)
        {
            logger.LogWarning(
                "SellerSelfShipSlaScheduler: Order {OrderId} has no PaidAt; skipping SLA stamp.",
                notification.OrderId);
            return;
        }

        // Re-derive flow: same indirection as OrderMappings.BuildSellerFulfillment.
        // warehouse_managed → WarehouseItem exists for the auctioned item in
        // (Received | Stored | Reserved). Otherwise seller_self_ship.
        var auction = await dbContext.Set<Auction>()
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == order.AuctionId, cancellationToken);

        if (auction?.Item is null)
        {
            logger.LogWarning(
                "SellerSelfShipSlaScheduler: Auction/item missing for Order {OrderId}; skipping.",
                notification.OrderId);
            return;
        }

        var itemIdGuid = auction.Item.Id.Value;
        var warehouseItem = await dbContext.Set<WarehouseItem>()
            .FirstOrDefaultAsync(
                wi => wi.ItemId == itemIdGuid &&
                      (wi.Status == WarehouseItemStatus.Stored ||
                       wi.Status == WarehouseItemStatus.Reserved ||
                       wi.Status == WarehouseItemStatus.Received),
                cancellationToken);

        if (warehouseItem is not null)
        {
            // warehouse_managed → no seller-ship SLA
            return;
        }

        var slaDays = runtimeSettings.Order.SellerShipSlaDays;
        if (slaDays <= 0)
        {
            logger.LogWarning(
                "SellerSelfShipSlaScheduler: SellerShipSlaDays is {SlaDays} (<=0); skipping SLA stamp for Order {OrderId} to avoid creating instantly-overdue orders.",
                slaDays, notification.OrderId);
            return;
        }

        var shipByAt = order.PaidAt.Value.AddDays(slaDays);
        var stamp = order.StampShipBySla(shipByAt);
        if (stamp.IsFailure)
        {
            logger.LogWarning(
                "SellerSelfShipSlaScheduler: StampShipBySla failed for Order {OrderId}: {Error}",
                notification.OrderId, stamp.Error.Message);
            return;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "SellerSelfShipSlaScheduler: Stamped ShipByAt={ShipByAt} for self-ship Order {OrderId}.",
            shipByAt, notification.OrderId);
    }
}
