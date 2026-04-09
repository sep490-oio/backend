using System.Text.Json;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;

namespace OIO.Application.Context.ModerationContext.Services;

internal static class DisputeContextSnapshotBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ForOrder(Order order) =>
        JsonSerializer.Serialize(new
        {
            orderNumber = order.OrderNumber.Value,
            orderStatus = order.Status.Id,
            buyerId = order.BuyerId.Value,
            sellerId = order.SellerId.Value,
            totalAmount = order.Pricing.TotalAmount.Amount,
            currency = order.Currency,
            auctionId = order.AuctionId.Value,
            createdAt = order.CreatedAt
        }, JsonOptions);

    public static string ForAuction(Auction auction) =>
        JsonSerializer.Serialize(new
        {
            auctionId = auction.Id.Value,
            auctionStatus = auction.Status.Id,
            itemId = auction.ItemId.Value,
            sellerId = auction.Item.SellerId.Value,
            winnerId = auction.WinnerId?.Value,
            createdAt = auction.CreatedAt
        }, JsonOptions);

    public static string ForTransaction(Transaction transaction) =>
        JsonSerializer.Serialize(new
        {
            transactionId = transaction.Id.Value,
            transactionNumber = transaction.TransactionNumber.Value,
            type = transaction.Type.Id,
            amount = transaction.Amount.Amount,
            currency = transaction.Currency,
            status = transaction.Status.Id,
            userId = transaction.UserId.Value,
            gatewayTransactionId = transaction.Gateway.TransactionId,
            createdAt = transaction.CreatedAt
        }, JsonOptions);

    public static string ForWarehouseItem(WarehouseItem warehouseItem) =>
        JsonSerializer.Serialize(new
        {
            warehouseItemId = warehouseItem.Id.Value,
            itemId = warehouseItem.ItemId,
            status = warehouseItem.Status.Id,
            inboundShipmentId = warehouseItem.InboundShipmentId.Value,
            createdAt = warehouseItem.CreatedAt
        }, JsonOptions);

    public static string ForOutboundShipment(OutboundShipment shipment) =>
        JsonSerializer.Serialize(new
        {
            shipmentId = shipment.Id.Value,
            orderId = shipment.OrderId.Value,
            status = shipment.Status.Id,
            providerCode = shipment.ProviderCode.Id,
            carrierTrackingNumber = shipment.CarrierTrackingNumber,
            createdAt = shipment.CreatedAt
        }, JsonOptions);
}
