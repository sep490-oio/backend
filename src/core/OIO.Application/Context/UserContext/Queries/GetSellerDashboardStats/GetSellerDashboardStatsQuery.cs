using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.UserContext.Queries.GetSellerDashboardStats;

public sealed record SellerDashboardStatsDto(
    int ActiveAuctions,
    int SoldAuctions,
    int DraftAuctions,
    int OrdersAwaitingShipment,
    int PendingReviewItems,
    int RejectedItems,
    int ActiveWarehouseReturns,
    int OrderReturns,
    decimal TotalRevenue,
    int TotalActiveBids,
    int TotalActiveViews
);

public sealed record GetSellerDashboardStatsQuery : IQuery<SellerDashboardStatsDto>;

internal sealed class GetSellerDashboardStatsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetSellerDashboardStatsQuery, SellerDashboardStatsDto>
{
    public async Task<Result<SellerDashboardStatsDto, Error>> Handle(
        GetSellerDashboardStatsQuery request,
        CancellationToken ct)
    {
        var sellerId = currentUser.UserId;

        // Auction Stats
        var auctionBaseQuery = dbContext.Set<Auction>().AsNoTracking().Where(a => a.Item.SellerId == sellerId);
        
        var activeAuctions = await auctionBaseQuery
            .Where(a => a.Status == AuctionStatus.Active || a.Status == AuctionStatus.Scheduled)
            .CountAsync(ct);

        var soldAuctions = await auctionBaseQuery
            .Where(a => a.Status == AuctionStatus.Sold || a.Status == AuctionStatus.Completed)
            .CountAsync(ct);

        var draftAuctions = await auctionBaseQuery
            .Where(a => a.Status == AuctionStatus.Draft)
            .CountAsync(ct);

        var activeAuctionsList = await auctionBaseQuery
            .Where(a => a.Status == AuctionStatus.Active)
            .Select(a => new { a.BidCount, a.ViewCount })
            .ToListAsync(ct);

        var totalActiveBids = activeAuctionsList.Sum(a => a.BidCount);
        var totalActiveViews = activeAuctionsList.Sum(a => a.ViewCount);

        // Order Stats
        var orderBaseQuery = dbContext.Set<Order>().AsNoTracking().Where(o => o.SellerId == sellerId);

        var ordersAwaitingShipment = await orderBaseQuery
            .Where(o => o.Status == OrderStatus.Paid)
            .CountAsync(ct);

        var revenueOrders = await orderBaseQuery
            .Where(o => o.Status == OrderStatus.Paid || 
                        o.Status == OrderStatus.Shipped || 
                        o.Status == OrderStatus.Delivered || 
                        o.Status == OrderStatus.Completed)
            .Select(o => o.Pricing.TotalAmount.Amount)
            .ToListAsync(ct);

        var totalRevenue = revenueOrders.Sum();

        var orderReturns = await dbContext.Set<OrderReturn>().AsNoTracking()
            .Where(r => r.Order.SellerId == sellerId && r.Status == OrderReturnStatus.Requested)
            .CountAsync(ct);

        // Item Stats
        var itemBaseQuery = dbContext.Set<Item>().AsNoTracking().Where(i => i.SellerId == sellerId);
        var pendingReviewItems = await itemBaseQuery
            .Where(i => i.Status == ItemStatus.PendingReview)
            .CountAsync(ct);
        var rejectedItems = await itemBaseQuery
            .Where(i => i.Status == ItemStatus.Rejected)
            .CountAsync(ct);

        // Warehouse Return Stats
        var activeWarehouseReturns = await dbContext.Set<WarehouseToSellerShipment>()
            .AsNoTracking()
            .Where(r => r.SellerId == sellerId && (
                r.Status == WarehouseToSellerShipmentStatus.Pending ||
                r.Status == WarehouseToSellerShipmentStatus.InTransit ||
                r.Status == WarehouseToSellerShipmentStatus.Delivered))
            .CountAsync(ct);

        return new SellerDashboardStatsDto(
            activeAuctions,
            soldAuctions,
            draftAuctions,
            ordersAwaitingShipment,
            pendingReviewItems,
            rejectedItems,
            activeWarehouseReturns,
            orderReturns,
            totalRevenue,
            totalActiveBids,
            totalActiveViews
        );
    }
}
