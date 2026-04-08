using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Application.Context.OrderContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.OrderContext.ValueObjects.Ids;
using SellerDirectShipmentEntity = OIO.Domain.Context.OrderContext.Aggregates.SellerDirectShipments.SellerDirectShipment;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Queries.GetSellerDirectShipOrders;

public sealed record GetSellerDirectShipOrdersQuery(PagedParameters Parameters) : IQuery<PagedList<OrderDto>>;

/// <summary>
/// Returns the seller's paid/processing orders enriched for the seller fulfillment UI.
///
/// Unlike the previous iteration (which hard-excluded warehouse-stored items), this
/// handler returns ALL seller orders in paid/processing and attaches a
/// <see cref="SellerFulfillmentDto"/> that tells the UI which shipment path applies:
///   - <c>book_outbound</c> → item lives in a non-dispatched warehouse slot
///   - <c>self_ship</c>     → no warehouse slot (or already dispatched)
///
/// The FE branches its "Create Shipment" CTA on <c>fulfillmentMode</c> and switches
/// to "View Shipment" when <c>hasActiveOutboundShipment</c> is true.
/// </summary>
internal sealed class GetSellerDirectShipOrdersQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser)
    : IQueryHandler<GetSellerDirectShipOrdersQuery, PagedList<OrderDto>>
{
    public async Task<Result<PagedList<OrderDto>, Error>> Handle(
        GetSellerDirectShipOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        // Seller's paid/processing orders — no warehouse-based exclusion, the
        // SellerFulfillmentDto tells the UI which shipment flow to use.
        var ordersQuery = dbContext.Set<Order>()
            .Where(o => o.SellerId == currentUser.UserId &&
                       (o.Status == OrderStatus.Paid ||
                        o.Status == OrderStatus.Processing ||
                        o.Status == OrderStatus.PickedUp ||
                        o.Status == OrderStatus.OnDelivering));

        var totalCounts = await ordersQuery.CountAsync(cancellationToken);

        var pagedOrders = await ordersQuery
            .Include(x => x.Return)
            .Include(x => x.Escrows)
            .Include(x => x.OutboundShipments)
            .OrderByDescending(x => x.CreatedAt)
            .Page(parameters)
            .ToListAsync(cancellationToken);

        // Batch-load related auctions + items so each OrderDto carries its product summary
        var auctionIds = pagedOrders.Select(o => o.AuctionId).Distinct().ToList();
        var auctionsById = new Dictionary<AuctionId, Auction>();
        if (auctionIds.Count > 0)
        {
            var auctions = await dbContext.Set<Auction>()
                .AsNoTracking()
                .Include(a => a.Item)
                    .ThenInclude(i => i.Media)
                .Where(a => auctionIds.Contains(a.Id))
                .ToListAsync(cancellationToken);

            auctionsById = auctions.ToDictionary(a => a.Id);
        }

        // Batch-load warehouse items so we can resolve fulfillment mode + warehouseItemId
        var itemGuidIds = auctionsById.Values
            .Select(a => a.Item.Id.Value)
            .Distinct()
            .ToList();

        var warehouseItemsByItemId = new Dictionary<Guid, WarehouseItem>();
        if (itemGuidIds.Count > 0)
        {
            // Include dispatched rows — ownership is determined by "ever
            // was in warehouse", not by current warehouse state. Excluding
            // Dispatched would silently flip orders back to seller_self_ship
            // after the outbound shipment left and re-expose seller
            // progression buttons on warehouse-fulfilled orders.
            var whItems = await dbContext.Set<WarehouseItem>()
                .AsNoTracking()
                .Where(wi => itemGuidIds.Contains(wi.ItemId))
                .ToListAsync(cancellationToken);

            // In case there are multiple historic rows per item, keep the most recent.
            warehouseItemsByItemId = whItems
                .GroupBy(wi => wi.ItemId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(wi => wi.CreatedAt).First());
        }

        // Batch-load direct shipments keyed by OrderId (1:1 with order).
        var orderIds = pagedOrders.Select(o => o.Id).ToList();
        var directShipmentsByOrderId = new Dictionary<OrderId, SellerDirectShipmentEntity>();
        if (orderIds.Count > 0)
        {
            var shipments = await dbContext.Set<SellerDirectShipmentEntity>()
                .AsNoTracking()
                .Include(s => s.Evidence)
                .Where(s => orderIds.Contains(s.OrderId))
                .ToListAsync(cancellationToken);
            directShipmentsByOrderId = shipments.ToDictionary(s => s.OrderId);
        }

        var dtos = pagedOrders
            .Select(o =>
            {
                var auction = auctionsById.GetValueOrDefault(o.AuctionId);
                var itemSummary = o.ToItemSummary(auction);
                WarehouseItem? wi = null;
                if (auction?.Item is not null)
                {
                    warehouseItemsByItemId.TryGetValue(auction.Item.Id.Value, out wi);
                }
                var fulfillment = o.BuildSellerFulfillment(wi);
                var directShipment = directShipmentsByOrderId.GetValueOrDefault(o.Id);
                return o.ToDto(item: itemSummary, sellerFulfillment: fulfillment, directShipment: directShipment);
            })
            .ToList();

        return dtos.ToPagedList(totalCounts, parameters);
    }
}
