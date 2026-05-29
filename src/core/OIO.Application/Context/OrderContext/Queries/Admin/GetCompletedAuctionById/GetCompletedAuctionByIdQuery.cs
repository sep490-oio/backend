using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ModerationContext.DTOs;
using OIO.Application.Context.ModerationContext.Mappings;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.OrderContext.Queries.Admin.GetCompletedAuctions;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using SellerDirectShipmentEntity = OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments.SellerDirectShipment;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Queries.Admin.GetCompletedAuctionById;

public sealed record GetCompletedAuctionByIdQuery(Guid AuctionId)
    : IQuery<AdminCompletedAuctionDetailDto>;

internal sealed class GetCompletedAuctionByIdQueryHandler(
    IDbContext dbContext,
    IClock clock)
    : IQueryHandler<GetCompletedAuctionByIdQuery, AdminCompletedAuctionDetailDto>
{
    public async Task<Result<AdminCompletedAuctionDetailDto, Error>> Handle(
        GetCompletedAuctionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);

        var auction = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
                .ThenInclude(i => i.Media)
            .FirstOrDefaultAsync(a => a.Id == auctionId, cancellationToken);

        if (auction is null)
            return Error.NotFound("Auction.NotFound", "Auction was not found.");

        // IsSuccessfullyClosed — pure C# post-read check, use helper directly.
        if (!auction.Status.IsSuccessfullyClosed)
            return Error.Validation("auctionId", "Auction.NotCompleted", "Auction has not been completed.");

        var order = await dbContext.Set<Order>()
            .AsNoTracking()
            .Include(o => o.Return)
            .Include(o => o.Escrows)
            .Include(o => o.OutboundShipments)
            .FirstOrDefaultAsync(o => o.AuctionId == auctionId, cancellationToken);


        // Resolve warehouse flow to match list derivation.
        WarehouseItem? warehouseItem = null;
        if (auction.Item is not null)
        {
            warehouseItem = await dbContext.Set<WarehouseItem>()
                .AsNoTracking()
                .Where(wi => wi.ItemId == auction.Item.Id.Value)
                .OrderByDescending(wi => wi.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }
        var flow = warehouseItem is not null ? "warehouse_managed" : "seller_self_ship";

        var users = await dbContext.Set<User>()
            .AsNoTracking()
            .Include(u => u.SellerProfile)
            .Where(u => u.Id == auction.WinnerId || u.Id == auction.Item!.SellerId)
            .ToListAsync(cancellationToken);
        var buyer = auction.WinnerId != null ? users.FirstOrDefault(u => u.Id == auction.WinnerId) : null;
        var seller = auction.Item != null ? users.FirstOrDefault(u => u.Id == auction.Item.SellerId) : null;
        var buyerDisplayName = GetCompletedAuctionsQueryHandler.ResolveUserDisplayName(buyer);
        var sellerDisplayName = GetCompletedAuctionsQueryHandler.ResolveSellerDisplayName(seller);

        // Build the list-row summary so the detail page can reuse the same
        // header card the table row shows.
        var nowUtc = clock.UtcNow;
        var primaryImageUrl = auction.Item?.Media
            .Where(m => m.IsPrimary)
            .OrderBy(m => m.SortOrder)
            .Select(m => m.Info.SecureUrl)
            .FirstOrDefault();

        string paymentStatus = "uncreated";
        string fulfillmentStatus = "uncreated";
        if (order != null)
        {
            paymentStatus = GetCompletedAuctionsQueryHandler.DerivePaymentStatus(order, nowUtc);
            fulfillmentStatus = GetCompletedAuctionsQueryHandler.DeriveFulfillmentStatus(order, flow);
        }

        var summary = new AdminCompletedAuctionListItemDto(
            AuctionId: auction.Id.Value,
            ItemTitle: auction.Item?.Title.Value ?? string.Empty,
            ItemPrimaryImageUrl: primaryImageUrl,
            WinnerId: auction.WinnerId?.Value,
            WinnerDisplayName: buyerDisplayName,
            SellerId: seller?.Id.Value ?? Guid.Empty,
            SellerDisplayName: sellerDisplayName,
            FinalPrice: order?.Pricing.ItemPrice.Amount ?? auction.Pricing.CurrentAmount,
            Currency: order?.Currency ?? auction.Pricing.Currency.Id,
            OrderId: order?.Id.Value,
            OrderNumber: order?.OrderNumber.Value,
            OrderStatus: order?.Status.Id,
            PaymentStatus: paymentStatus,
            FulfillmentFlow: flow,
            FulfillmentStatus: fulfillmentStatus,
            PaymentDueAt: order?.PaymentDueAt,
            PaidAt: order?.PaidAt,
            ShipByAt: order?.ShipByAt,
            IsShippingOverdue: order?.IsShippingOverdue,
            EscalatedAt: order?.EscalatedAt,
            EscalationReason: order?.EscalationReason,
            CreatedAt: order?.CreatedAt);

        OrderDto? orderDto = null;
        if (order != null)
        {
            var itemSummary = order.ToItemSummary(auction);
            var sellerFulfillment = order.BuildSellerFulfillment(warehouseItem);

            // 1:1 seller direct shipment (if any) — admin needs this to render
            // the DirectShipmentAdminBlock on the completed-auction detail page.
            var directShipment = await dbContext.Set<SellerDirectShipmentEntity>()
                .AsNoTracking()
                .Include(s => s.Evidence)
                .FirstOrDefaultAsync(s => s.OrderId == order.Id, cancellationToken);

            orderDto = order.ToDto(
                item: itemSummary,
                sellerFulfillment: sellerFulfillment,
                buyerDisplayName: buyerDisplayName,
                sellerDisplayName: sellerDisplayName,
                directShipment: directShipment);
        }

        // Attach latest outbound shipment DTO (null for self-ship orders
        // that have not been picked up yet).
        OutboundShipmentDto? outboundDto = null;
        if (order != null)
        {
            var latestShipment = order.OutboundShipments
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefault();
            if (latestShipment is not null)
                outboundDto = latestShipment.ToDto();
        }

        // Monitoring alerts keyed by order (EntityType="order") — this is the
        // same entity filter the shared MonitoringAlert query uses, so admin
        // operators see the exact same rows here as on the alert triage page.
        var monitoringAlerts = new List<MonitoringAlertDto>();
        if (order != null)
        {
            var orderGuid = order.Id.Value;
            monitoringAlerts = await dbContext.Set<MonitoringAlert>()
                .AsNoTracking()
                .Where(x => x.EntityType == "Order" && x.EntityId == orderGuid)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.ToDto())
                .ToListAsync(cancellationToken);
        }

        return new AdminCompletedAuctionDetailDto(
            Summary: summary,
            Order: orderDto,
            OutboundShipment: outboundDto,
            MonitoringAlerts: monitoringAlerts);
    }
}
