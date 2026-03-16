using System.Linq;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;

namespace OIO.Application.Context.OrderContext.Mappings;

internal static class OrderMappings
{
    public static OrderDto ToDto(this Order order)
    {
        var escrowStatus =
            order.Escrows.Any(x => x.Status.Id == "holding") ? "holding" :
            order.Escrows.Any(x => x.Status.Id == "released_to_seller") ? "released_to_seller" :
            order.Escrows.Any(x => x.Status.Id == "refunded_to_buyer") ? "refunded_to_buyer" :
            null;
        var shipment = order.OutboundShipments.OrderByDescending(x => x.CreatedAt).FirstOrDefault();

        return new OrderDto(
            Id: order.Id.Value,
            OrderNumber: order.OrderNumber.Value,
            AuctionId: order.AuctionId.Value,
            BuyerId: order.BuyerId.Value,
            SellerId: order.SellerId.Value,
            Status: order.Status.Id,
            TotalAmount: order.Pricing.TotalAmount.Amount,
            Currency: order.Currency,
            CreatedAt: order.CreatedAt,
            PaymentDueAt: order.PaymentDueAt,
            PaidAt: order.PaidAt,
            ShippedAt: order.ShippedAt,
            DeliveredAt: order.DeliveredAt,
            DecisionWindowEndsAt: order.DecisionWindowEndsAt,
            CompletedAt: order.CompletedAt,
            CancelledAt: order.CancelledAt,
            EscrowStatus: escrowStatus,
            TrackingNumber: shipment?.CarrierTrackingNumber,
            Return: order.Return?.ToDto());
    }

    public static OrderReturnDto ToDto(this OrderReturn orderReturn)
    {
        return new OrderReturnDto(
            Id: orderReturn.Id.Value,
            Status: orderReturn.Status.Id,
            ReasonCode: orderReturn.ReasonCode,
            Description: orderReturn.Description,
            DecisionReason: orderReturn.DecisionReason,
            ProviderCode: orderReturn.ProviderCode,
            TrackingNumber: orderReturn.TrackingNumber,
            RequestedAt: orderReturn.RequestedAt,
            ApprovedAt: orderReturn.ApprovedAt,
            RejectedAt: orderReturn.RejectedAt,
            ShippedAt: orderReturn.ShippedAt,
            SellerReceivedAt: orderReturn.SellerReceivedAt,
            BuyerDecisionDueAt: orderReturn.BuyerDecisionDueAt);
    }
}
