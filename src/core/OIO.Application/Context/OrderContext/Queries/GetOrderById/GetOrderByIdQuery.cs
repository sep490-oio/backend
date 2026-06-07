using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using SellerDirectShipmentEntity = OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments.SellerDirectShipment;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using OIO.Domain.Context.PaymentContext.Aggregates.Transactions;
using OIO.Domain.Context.PaymentContext.Descriptions;
using OIO.Domain.Context.PaymentContext.Enums;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Application.Context.WarehouseContext.Queries.GetBuyerOutboundShipmentByToken;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId) : IQuery<OrderDto>;

internal sealed class GetOrderByIdQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetOrderByIdQuery, OrderDto>
{
    public async Task<Result<OrderDto, Error>> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var order = await dbContext.Set<Order>()
            .AsNoTracking()
            .Include(x => x.Return)
                .ThenInclude(r => r!.Evidence)
            .Include(x => x.Escrows)
            .Include(x => x.OutboundShipments)
            .FirstOrDefaultAsync(x => x.Id == OrderId.From(request.OrderId), cancellationToken);

        if (order is null)
            return Error.NotFound("Order.NotFound", "Order was not found.");

        if (order.BuyerId != currentUser.UserId &&
            order.SellerId != currentUser.UserId)
        {
            return Error.Forbidden("Order.Forbidden", "You are not allowed to access this order.");
        }

        // Load the related auction + item so we can attach a compact product summary.
        // A single extra query keeps this feature additive without refactoring the
        // Order query shape. If the auction has been removed, the summary is null
        // and the FE renders its placeholder.
        var auction = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
                .ThenInclude(i => i.Media)
            .FirstOrDefaultAsync(a => a.Id == order.AuctionId, cancellationToken);

        var itemSummary = order.ToItemSummary(auction);

        // Seller-only: attach fulfillment metadata so the seller's order detail
        // page can render the same confirm/create-shipment/view-shipment action
        // panel as /seller/orders. Buyer view doesn't need this payload.
        SellerFulfillmentDto? sellerFulfillment = null;
        if (order.SellerId == currentUser.UserId && auction?.Item is not null)
        {
            var itemGuid = auction.Item.Id.Value;
            // Include dispatched warehouse items too — ANY warehouse row
            // proves the item lived in the platform warehouse, which makes
            // the order warehouse_managed even after the outbound shipment
            // has left. Excluding Dispatched would flip the seller action
            // matrix back to self-ship and let seller click progression
            // buttons on an order the warehouse fulfilled.
            var warehouseItem = await dbContext.Set<WarehouseItem>()
                .AsNoTracking()
                .Where(wi => wi.ItemId == itemGuid)
                .OrderByDescending(wi => wi.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            sellerFulfillment = order.BuildSellerFulfillment(warehouseItem);
        }

        // Buyer-scoped: allow shipping edits from detail page when the viewer
        // is the buyer and the order is still pre-fulfillment (paid or pending).
        var buyerCanUpdateShipping =
            order.BuyerId == currentUser.UserId &&
            (order.Status == OIO.Domain.Context.OrderContext.Enums.OrderStatus.PendingPayment ||
             order.Status == OIO.Domain.Context.OrderContext.Enums.OrderStatus.Paid);

        // Resolve display names — BuyerDisplayName uses profile display/full
        // name then username; SellerDisplayName additionally prefers
        // SellerProfile.StoreName. Detail and list must share the same shape.
        var users = await dbContext.Set<User>()
            .AsNoTracking()
            .Include(u => u.SellerProfile)
            .Where(u => u.Id == order.BuyerId || u.Id == order.SellerId)
            .ToListAsync(cancellationToken);

        var buyer = users.FirstOrDefault(u => u.Id == order.BuyerId);
        var seller = users.FirstOrDefault(u => u.Id == order.SellerId);
        var buyerDisplayName = ResolveUserDisplayName(buyer);
        var sellerDisplayName = ResolveSellerDisplayName(seller);

        // Load completed transactions for this order so the DTO can expose
        // a payment-method breakdown (deposit / wallet / gateway). FE escrow
        // timeline renders these without a second round-trip.
        var orderIdValue = order.Id;
        var orderTransactions = await dbContext.Set<Transaction>()
            .AsNoTracking()
            .Where(t => t.OrderId == orderIdValue && t.Status == TransactionStatus.Completed)
            .ToListAsync(cancellationToken);

        // Seller direct-ship shipment is a separate aggregate (no navigation
        // on Order). Load the 1:1 row by FK so OrderDto can expose it.
        // Load the linked buy-now reservation (if any) so the DTO can surface
        // the deposit-applied offset immediately — even before the ledger posts
        // the AuctionBuyNowDepositApplied transaction (pending_payment state).
        var buyNowReservation = await dbContext.Set<AuctionBuyNowReservation>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrderId == order.Id, cancellationToken);

        var directShipment = await dbContext.Set<SellerDirectShipmentEntity>()
            .AsNoTracking()
            .Include(s => s.Evidence)
            .FirstOrDefaultAsync(s => s.OrderId == order.Id, cancellationToken);

        // Normal auction winner deposit: load the held/converted deposit so
        // the mapping can display the offset on the checkout / order detail
        // page even before payment completes.
        decimal? winnerDepositAmount = null;
        if (buyNowReservation is null)
        {
            var winnerDeposit = await dbContext.Set<AuctionDeposit>()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    d => d.AuctionId == order.AuctionId &&
                         d.BidderId == order.BuyerId &&
                         (d.Status == DepositStatus.Held || d.Status == DepositStatus.ConvertedToPayment),
                    cancellationToken);
            winnerDepositAmount = winnerDeposit?.Amount.Amount;
        }

        // Buyer-scoped warehouse outbound snapshot. Only populated for the
        // order buyer and when an active (non-terminal) outbound shipment
        // exists. Mirrors BuyerOutboundShipmentDetailBuilder's flag logic so
        // the order detail page renders the same action hub as the QR deep
        // link page.
        OrderWarehouseOutboundShipmentDto? warehouseOutboundDto = null;
        if (order.BuyerId == currentUser.UserId)
        {
            var activeOutbound = order.OutboundShipments
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault(s => BuyerOutboundShipmentDetailBuilder.IsAllowedShipmentStatus(s.Status));
            if (activeOutbound is not null)
            {
                var detail = await BuyerOutboundShipmentDetailBuilder.BuildAsync(
                    dbContext, activeOutbound, order, cancellationToken);
                // QrPayload guarded by buyer ownership (mirrors the token
                // query). Since this branch already requires the viewer is
                // the buyer, always expose it.
                warehouseOutboundDto = new OrderWarehouseOutboundShipmentDto(
                    ShipmentId: activeOutbound.Id.Value,
                    Status: activeOutbound.Status.Id,
                    ShipmentMode: activeOutbound.ShipmentMode.Id,
                    ProviderCode: activeOutbound.ProviderCode.Id,
                    ExternalCarrierName: activeOutbound.ExternalCarrierName,
                    CarrierTrackingNumber: activeOutbound.CarrierTrackingNumber,
                    ClientOrderCode: activeOutbound.ClientOrderCode,
                    QrPayload: activeOutbound.QrPayload,
                    QrAvailable: detail.QrAvailable,
                    DispatchedAt: activeOutbound.DispatchedAt,
                    DeliveredAt: activeOutbound.DeliveredAt,
                    BuyerReceivedPackageAt: activeOutbound.BuyerReceivedPackageAt,
                    BuyerAcceptedAt: activeOutbound.BuyerAcceptedAt,
                    CanAcknowledgeReceived: false,
                    CanAccept: detail.CanAccept,
                    CanOpenDispute: detail.CanOpenDispute,
                    HasActiveDispute: detail.HasActiveDispute,
                    DecisionWindowEndsAt: order.DecisionWindowEndsAt,
                    CanSubmitProof: false,
                    HasBuyerReceiptProof: detail.HasBuyerReceiptProof,
                    CanSubmitReceiptProof: false);
            }
        }

        // Hybrid wallet+VNPay payments create WalletTransaction entries (not
        // Transaction entities) when the wallet portion is committed. Query the
        // buyer's wallet ledger for the [HybridHold] marker to resolve the
        // wallet portion amount for the DTO breakdown.
        decimal? hybridWalletHoldAmount = null;
        var orderIdStr = order.Id.Value.ToString();
        var hybridHoldTag = LedgerTags.Bracket(LedgerTags.HybridHold);
        var hybridHoldTx = await dbContext.Set<OIO.Domain.Context.PaymentContext.Aggregates.Wallets.WalletTransaction>()
            .AsNoTracking()
            .Where(wt => wt.Description != null &&
                         wt.Description.Contains(hybridHoldTag) &&
                         wt.Description.Contains(orderIdStr))
            .OrderByDescending(wt => wt.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (hybridHoldTx is not null)
            hybridWalletHoldAmount = hybridHoldTx.Amount;

        return order.ToDto(
            item: itemSummary,
            sellerFulfillment: sellerFulfillment,
            buyerCanUpdateShipping: buyerCanUpdateShipping,
            buyerDisplayName: buyerDisplayName,
            sellerDisplayName: sellerDisplayName,
            orderTransactions: orderTransactions,
            directShipment: directShipment,
            warehouseOutboundShipment: warehouseOutboundDto,
            buyNowReservation: buyNowReservation,
            winnerDepositAmount: winnerDepositAmount,
            hybridWalletHoldAmount: hybridWalletHoldAmount);
    }

    private static string? ResolveUserDisplayName(User? user)
    {
        if (user is null) return null;
        var profile = user.Profile;
        if (profile?.Name is not null)
        {
            if (!string.IsNullOrWhiteSpace(profile.Name.DisplayName)) return profile.Name.DisplayName;
            if (!string.IsNullOrWhiteSpace(profile.Name.FullName)) return profile.Name.FullName;
        }
        return user.UserName?.Value;
    }

    private static string? ResolveSellerDisplayName(User? user)
    {
        if (user is null) return null;
        if (user.SellerProfile is not null && !string.IsNullOrWhiteSpace(user.SellerProfile.StoreName))
            return user.SellerProfile.StoreName;
        return ResolveUserDisplayName(user);
    }
}
