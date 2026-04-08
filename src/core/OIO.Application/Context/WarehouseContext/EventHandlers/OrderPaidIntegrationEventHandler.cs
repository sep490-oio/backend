using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.WarehouseContext.Commands.BookOutboundShipment;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;

namespace OIO.Application.Context.WarehouseContext.EventHandlers;

public sealed record OrderPaidIntegrationEvent(Guid OrderId) : INotification;

internal sealed class OrderPaidIntegrationEventHandler(
    ISender sender,
    IDbContext dbContext,
    ILogger<OrderPaidIntegrationEventHandler> logger)
    : INotificationHandler<OrderPaidIntegrationEvent>
{
    public async Task Handle(OrderPaidIntegrationEvent notification, CancellationToken cancellationToken)
    {
        // Ownership change: warehouse-managed orders must wait for warehouse
        // staff to book outbound manually. Auto-booking on Order.Paid is
        // disabled; this handler is kept as a stub so integration-event
        // plumbing continues to compile, but no side-effects run.
        logger.LogInformation(
            "OrderPaidIntegrationEvent received for OrderId: {OrderId}. Auto-book outbound is disabled — warehouse staff will book manually.",
            notification.OrderId);
        await Task.CompletedTask;
        return;

#pragma warning disable CS0162 // unreachable: reference implementation kept for warehouse-staff context extraction
        var orderId = OrderId.From(notification.OrderId);

        // 1. Load order with shipping info
        var order = await dbContext.Set<Order>()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order {OrderId} not found. Skipping outbound shipment.", notification.OrderId);
            return;
        }

        // 2. Load auction + item
        var auction = await dbContext.Set<Auction>()
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => a.Id == order.AuctionId, cancellationToken);

        if (auction is null)
        {
            logger.LogWarning("Auction {AuctionId} not found for Order {OrderId}. Skipping.", order.AuctionId, notification.OrderId);
            return;
        }

        // 3. Find warehouse item for this item (Stored, Reserved, or Received status)
        var itemIdGuid = auction.Item.Id.Value;
        var warehouseItem = await dbContext.Set<WarehouseItem>()
            .FirstOrDefaultAsync(
                wi => wi.ItemId == itemIdGuid &&
                      (wi.Status == WarehouseItemStatus.Stored ||
                       wi.Status == WarehouseItemStatus.Reserved ||
                       wi.Status == WarehouseItemStatus.Received),
                cancellationToken);

        if (warehouseItem is null)
        {
            logger.LogWarning(
                "No stored WarehouseItem found for ItemId {ItemId} (Order {OrderId}). Outbound shipment must be booked manually.",
                itemIdGuid, notification.OrderId);
            return;
        }

        // 4. Get inbound shipment for weight/dimension info
        var inboundShipment = await dbContext.Set<InboundShipment>()
            .FirstOrDefaultAsync(s => s.Id == warehouseItem.InboundShipmentId, cancellationToken);

        var weightGrams = inboundShipment?.Dimensions.WeightGrams ?? 500;

        // 5. Build and send BookOutboundShipmentCommand
        var command = new BookOutboundShipmentCommand(
            OrderId: order.Id.Value,
            WarehouseItemId: warehouseItem.Id.Value,
            RecipientName: order.Shipping.RecipientName ?? "N/A",
            RecipientPhone: order.Shipping.Phone ?? "N/A",
            RecipientAddress: order.Shipping.Address,
            RecipientWard: order.Shipping.Ward ?? "",
            RecipientDistrict: order.Shipping.District ?? "",
            RecipientProvince: order.Shipping.City ?? "",
            WeightGrams: weightGrams,
            InsuranceValue: order.Pricing.TotalAmount.Amount,
            CodAmount: 0,
            ItemName: auction.Item.Title.Value,
            ItemPrice: order.Pricing.TotalAmount.Amount,
            LengthCm: inboundShipment?.Dimensions.LengthCm,
            WidthCm: inboundShipment?.Dimensions.WidthCm,
            HeightCm: inboundShipment?.Dimensions.HeightCm);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError(
                "Failed to auto-book outbound shipment for Order {OrderId}: {Error}",
                notification.OrderId, result.Error.Message);
        }
        else
        {
            logger.LogInformation(
                "Successfully auto-booked outbound shipment for Order {OrderId}, WarehouseItem {WarehouseItemId}.",
                notification.OrderId, warehouseItem.Id.Value);
        }
#pragma warning restore CS0162
    }
}
