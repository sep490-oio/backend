using System.Linq;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;

namespace OIO.Application.Context.OrderContext.Mappings;

internal static class OrderMappings
{
    public static OrderDto ToDto(
        this Order order,
        OrderItemSummaryDto? item = null,
        SellerFulfillmentDto? sellerFulfillment = null,
        bool buyerCanUpdateShipping = false,
        string? buyerDisplayName = null,
        string? sellerDisplayName = null,
        IReadOnlyList<Transaction>? orderTransactions = null,
        SellerDirectShipment? directShipment = null,
        OrderWarehouseOutboundShipmentDto? warehouseOutboundShipment = null,
        AuctionBuyNowReservation? buyNowReservation = null)
    {
        // Amount paid from escrows (holding or released_to_seller). Null when
        // no escrows exist so FE can distinguish "unpaid" from "0".
        decimal? amountPaid = null;
        var payingEscrows = order.Escrows
            .Where(e => e.Status.Id == "holding" || e.Status.Id == "released_to_seller")
            .ToList();
        if (payingEscrows.Count > 0)
        {
            amountPaid = payingEscrows.Sum(e => e.Amount.Amount);
        }

        decimal? depositAppliedAmount = null;
        decimal? walletAppliedAmount = null;
        decimal? gatewayPaidAmount = null;
        if (orderTransactions is not null)
        {
            var completed = orderTransactions
                .Where(t => t.Status == TransactionStatus.Completed)
                .ToList();

            var depositTxs = completed
                .Where(t =>
                    (t.Description != null && t.Description.Contains("[AuctionBuyNowDepositApplied]")) ||
                    t.TransactionNumber.Value.StartsWith("BNDEP-"))
                .ToList();
            if (depositTxs.Count > 0)
                depositAppliedAmount = depositTxs.Sum(t => t.Amount.Amount);

            var walletTxs = completed
                .Where(t =>
                    (t.Description != null && t.Description.StartsWith("[HybridHold] Wallet portion committed")) ||
                    t.TransactionNumber.Value.StartsWith("WLT-"))
                .ToList();
            if (walletTxs.Count > 0)
                walletAppliedAmount = walletTxs.Sum(t => t.Amount.Amount);

            var depositIds = depositTxs.Select(t => t.Id).ToHashSet();
            var walletIds = walletTxs.Select(t => t.Id).ToHashSet();
            var gatewayTxs = completed
                .Where(t =>
                    !depositIds.Contains(t.Id) &&
                    !walletIds.Contains(t.Id) &&
                    t.Gateway.Provider == "vnpay" &&
                    t.Amount.Amount > 0)
                .ToList();
            if (gatewayTxs.Count > 0)
                gatewayPaidAmount = gatewayTxs.Sum(t => t.Amount.Amount);
        }

        // Buy-now reservation offset: when the ledger hasn't posted the
        // AuctionBuyNowDepositApplied transaction yet (pending_payment state),
        // fall back to the live reservation's DepositAppliedAmount so the FE
        // sees the offset immediately from the moment the buy-now order is
        // created.
        if (depositAppliedAmount is null &&
            buyNowReservation is not null &&
            buyNowReservation.DepositAppliedAmountValue > 0m)
        {
            depositAppliedAmount = buyNowReservation.DepositAppliedAmountValue;
        }

        var escrowStatus =
            order.Escrows.Any(x => x.Status.Id == "holding") ? "holding" :
            order.Escrows.Any(x => x.Status.Id == "released_to_seller") ? "released_to_seller" :
            order.Escrows.Any(x => x.Status.Id == "refunded_to_buyer") ? "refunded_to_buyer" :
            null;
        var shipment = order.OutboundShipments.OrderByDescending(x => x.CreatedAt).FirstOrDefault();

        OrderShippingDto? shippingDto = null;
        if (order.Shipping is not null)
        {
            shippingDto = new OrderShippingDto(
                RecipientName: order.Shipping.RecipientName,
                PhoneNumber: order.Shipping.Phone,
                Street: order.Shipping.Street,
                Ward: order.Shipping.Ward,
                District: order.Shipping.District,
                City: order.Shipping.City,
                PostalCode: order.Shipping.PostalCode,
                ComposedAddress: order.Shipping.Address,
                IsStructured: order.Shipping.IsStructured);
        }

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
            Return: order.Return?.ToDto(),
            BuyerDisplayName: buyerDisplayName,
            SellerDisplayName: sellerDisplayName,
            Shipping: shippingDto,
            Item: item,
            SellerFulfillment: sellerFulfillment,
            BuyerCanUpdateShipping: buyerCanUpdateShipping,
            AmountPaid: amountPaid,
            DepositAppliedAmount: depositAppliedAmount,
            WalletAppliedAmount: walletAppliedAmount,
            GatewayPaidAmount: gatewayPaidAmount,
            DirectShipment: directShipment?.ToDto(),
            WarehouseOutboundShipment: warehouseOutboundShipment);
    }

    /// <summary>
    /// Builds an <see cref="OrderItemSummaryDto"/> from the order's related
    /// auction + item. Returns null when auction is null (e.g., legacy orders
    /// with a stale auction reference). PrimaryImageUrl is null when the item
    /// has no primary media; the FE falls back to a placeholder.
    /// </summary>
    /// <summary>
    /// Builds a seller-scope fulfillment summary for an Order. Shared by
    /// GetOrderById (detail) and GetSellerDirectShipOrders (list) so both
    /// screens agree on the action CTA (confirm / create shipment / view shipment).
    /// </summary>
    public static SellerFulfillmentDto BuildSellerFulfillment(
        this Order order,
        WarehouseItem? warehouseItem)
    {
        var activeShipment = order.OutboundShipments
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault(s =>
                s.Status != OutboundShipmentStatus.Delivered &&
                s.Status != OutboundShipmentStatus.Failed &&
                s.Status != OutboundShipmentStatus.Returned &&
                s.Status != OutboundShipmentStatus.Cancelled);

        // Ownership-aware flow detection:
        //   warehouse_managed  → goods sit in the platform warehouse (has WarehouseItem);
        //                        only warehouse staff may book outbound.
        //   seller_self_ship   → no warehouse item; seller ships directly to buyer.
        var flow = warehouseItem is not null ? "warehouse_managed" : "seller_self_ship";
        var sellerCanCreateShipment = flow == "seller_self_ship" && activeShipment is null;
        var warehouseStaffMustBookOutbound = flow == "warehouse_managed" && activeShipment is null;

        return new SellerFulfillmentDto(
            // Legacy FulfillmentMode kept for back-compat — mirrors FulfillmentFlow.
            FulfillmentMode: flow,
            FulfillmentFlow: flow,
            SellerCanCreateShipment: sellerCanCreateShipment,
            WarehouseStaffMustBookOutbound: warehouseStaffMustBookOutbound,
            WarehouseItemId: warehouseItem?.Id.Value,
            HasActiveOutboundShipment: activeShipment is not null,
            OutboundShipmentId: activeShipment?.Id.Value,
            PackageDefaults: null,
            ShipByAt: order.ShipByAt,
            IsShippingOverdue: order.IsShippingOverdue,
            EscalatedAt: order.EscalatedAt,
            EscalationReason: order.EscalationReason);
    }

    public static OrderItemSummaryDto? ToItemSummary(this Order order, OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Auction? auction)
    {
        if (auction?.Item is null) return null;

        var primaryImageUrl = auction.Item.Media
            .Where(m => m.IsPrimary)
            .OrderBy(m => m.SortOrder)
            .Select(m => m.Info.SecureUrl)
            .FirstOrDefault();

        return new OrderItemSummaryDto(
            ItemId: auction.Item.Id.Value,
            AuctionId: auction.Id.Value,
            ItemTitle: auction.Item.Title.Value,
            PrimaryImageUrl: primaryImageUrl,
            StartingPrice: auction.Pricing.StartingPrice.Amount,
            // Locked hammer price — use the order's item price (winning bid amount),
            // not total (which includes fees/shipping). Fall back to auction current price.
            FinalPrice: order.Pricing.ItemPrice.Amount,
            Currency: order.Currency);
    }

    public static SellerDirectShipmentDto ToDto(this SellerDirectShipment shipment)
    {
        static ShipmentEvidenceDto ToEvidenceDto(SellerDirectShipmentEvidence e) =>
            new(e.Id.Value, e.MediaUploadId.Value, e.MediaUrl, e.CreatedAt);

        var sellerPackagePhotos = shipment.Evidence
            .Where(e => e.Kind == SellerDirectShipmentEvidenceKind.SellerPackagePhoto)
            .OrderBy(e => e.CreatedAt)
            .Select(ToEvidenceDto)
            .ToList();

        var sellerHandoverProofs = shipment.Evidence
            .Where(e => e.Kind == SellerDirectShipmentEvidenceKind.SellerHandoverProof)
            .OrderBy(e => e.CreatedAt)
            .Select(ToEvidenceDto)
            .ToList();

        var buyerDeliveryPhotos = shipment.Evidence
            .Where(e => e.Kind == SellerDirectShipmentEvidenceKind.BuyerDeliveryPhoto)
            .OrderBy(e => e.CreatedAt)
            .Select(ToEvidenceDto)
            .ToList();

        return new SellerDirectShipmentDto(
            Id: shipment.Id.Value,
            OrderId: shipment.OrderId.Value,
            ShipmentIdDisplay: shipment.ShipmentIdDisplay,
            InternalTrackingCode: shipment.InternalTrackingCode,
            QrPayload: shipment.QrPayload,
            QrCodeUrl: shipment.QrCodeUrl,
            ExternalCarrierName: shipment.ExternalCarrierName,
            ExternalTrackingCode: shipment.ExternalTrackingCode,
            Status: shipment.Status.Id,
            CreatedAt: shipment.CreatedAt,
            ModifiedAt: shipment.ModifiedAt,
            CarrierBookedAt: shipment.CarrierBookedAt,
            PickedUpAt: shipment.PickedUpAt,
            OnDeliveringAt: shipment.OnDeliveringAt,
            DeliveredAt: shipment.DeliveredAt,
            BuyerReceivedPackageAt: shipment.BuyerReceivedPackageAt,
            BuyerAcceptedAt: shipment.BuyerAcceptedAt,
            DisputedAt: shipment.DisputedAt,
            CompletedAt: shipment.CompletedAt,
            SellerDeclaredShippedAt: shipment.SellerDeclaredShippedAt,
            SellerPackagePhotos: sellerPackagePhotos,
            SellerHandoverProofs: sellerHandoverProofs,
            BuyerDeliveryPhotos: buyerDeliveryPhotos,
            BuyerPackageCondition: shipment.BuyerPackageCondition,
            BuyerConditionNotes: shipment.BuyerConditionNotes,
            ManualReviewRequired: shipment.ManualReviewRequired,
            ManualReviewReason: shipment.ManualReviewReason,
            QrTokenVersion: shipment.QrTokenVersion,
            QrTokenIssuedAt: shipment.QrTokenIssuedAt,
            QrTokenRevokedAt: shipment.QrTokenRevokedAt);
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
            BuyerDecisionDueAt: orderReturn.BuyerDecisionDueAt,
            QrToken: orderReturn.QrToken,
            Evidence: orderReturn.Evidence?
                .Select(e => new OrderReturnEvidenceDto(
                    Id:            e.Id.Value,
                    OrderReturnId: e.OrderReturnId.Value,
                    Category:      e.Category,
                    MediaUpload:   new OrderReturnEvidenceMediaDto(
                        Id:           e.MediaUploadId.Value,
                        SecureUrl:    e.SecureUrl,
                        FileName:     e.FileName,
                        ResourceType: e.ResourceType),
                    CreatedAt:     e.CreatedAt,
                    CreatedBy:     e.CreatedBy.Value))
                .ToList()
                ?? new List<OrderReturnEvidenceDto>());
    }
}
