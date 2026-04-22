using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.OrderContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.OrderContext.Queries.Admin.GetCompletedAuctions;

public record GetCompletedAuctionsQueryFilter : PagedParameters
{
    // pending_payment | paid | payment_overdue
    public string? PaymentStatus { get; init; }
    // awaiting_seller_ship | warehouse_outbound_pending | picked_up | on_delivering
    // | delivered | shipping_overdue | escalated
    public string? FulfillmentStatus { get; init; }
    public bool? OnlyOverdue { get; init; }
    public string? Search { get; init; }
}

public sealed record GetCompletedAuctionsQuery(GetCompletedAuctionsQueryFilter Parameters)
    : IQuery<PagedList<AdminCompletedAuctionListItemDto>>;

internal sealed class GetCompletedAuctionsQueryHandler(
    IDbContext dbContext,
    IClock clock)
    : IQueryHandler<GetCompletedAuctionsQuery, PagedList<AdminCompletedAuctionListItemDto>>
{
    public async Task<Result<PagedList<AdminCompletedAuctionListItemDto>, Error>> Handle(
        GetCompletedAuctionsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var pageNumber = parameters.PageNumber ?? 1;
        var pageSize = parameters.PageSize ?? 20;
        var nowUtc = clock.UtcNow;

        // Base: auctions that are successfully closed — Sold (winner resolved)
        // or Completed (delivery confirmed, terminal). Includes buy-now which also
        // transitions to AuctionStatus.Sold. Orders are joined via AuctionId —
        // every successfully-closed auction has exactly one order created at close time.
        // IsSuccessfullyClosed expansion — EF translation requires the inlined predicate.
        var auctionsQuery = dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
                .ThenInclude(i => i.Media)
            .Where(a => a.Status == AuctionStatus.Sold || a.Status == AuctionStatus.Completed);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var needle = $"%{parameters.Search.Trim()}%";
            auctionsQuery = auctionsQuery.Where(a => EF.Functions.ILike(a.Item.Title.Value, needle));
        }

        // Push the easy filter (OnlyOverdue) into SQL via the Order join.
        // PaymentStatus / FulfillmentStatus require derivation, so they stay
        // in memory below.
        if (parameters.OnlyOverdue == true)
        {
            auctionsQuery = auctionsQuery.Where(a =>
                dbContext.Set<Order>().Any(o => o.AuctionId == a.Id && o.IsShippingOverdue));
        }

        // Join with orders in-memory after paging on the auction side — the
        // ordering key (auction.ModifiedAt / CreatedAt) is stable across filters.
        var orderedAuctionsQuery = auctionsQuery.OrderByDescending(a => a.ModifiedAt ?? a.CreatedAt);

        // Pull candidate auction ids matching the title/overdue filters, then
        // hydrate their orders and derive payment/fulfillment status filters in
        // memory. Hard cap at 5000 rows as a circuit breaker; admin completed-
        // auction screens are low-volume so this ceiling will not be reached in
        // practice. Full SQL derivation of the derived statuses is intentionally
        // out of scope for this fix pass.
        var candidateAuctions = await orderedAuctionsQuery
            .Take(5000)
            .ToListAsync(cancellationToken);

        var auctionIds = candidateAuctions.Select(a => a.Id).ToList();
        if (auctionIds.Count == 0)
        {
            return new PagedList<AdminCompletedAuctionListItemDto>(
                Array.Empty<AdminCompletedAuctionListItemDto>(), 0, pageNumber, pageSize);
        }

        var orders = await dbContext.Set<Order>()
            .AsNoTracking()
            .Include(o => o.OutboundShipments)
            .Where(o => auctionIds.Contains(o.AuctionId))
            .ToListAsync(cancellationToken);
        var ordersByAuction = orders.ToDictionary(o => o.AuctionId);

        // Load display names for winners + sellers in one hop.
        var userIds = orders
            .SelectMany(o => new[] { o.BuyerId, o.SellerId })
            .Distinct()
            .ToList();
        var users = await dbContext.Set<User>()
            .AsNoTracking()
            .Include(u => u.SellerProfile)
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
        var usersById = users.ToDictionary(u => u.Id.Value);

        // Warehouse items per auction.Item — any row marks the auction as
        // warehouse_managed (matches OrderMappings.BuildSellerFulfillment).
        var itemGuids = candidateAuctions
            .Where(a => a.Item is not null)
            .Select(a => a.Item.Id.Value)
            .Distinct()
            .ToList();
        var warehouseItemItemIds = new HashSet<Guid>();
        if (itemGuids.Count > 0)
        {
            warehouseItemItemIds = (await dbContext.Set<WarehouseItem>()
                .AsNoTracking()
                .Where(wi => itemGuids.Contains(wi.ItemId))
                .Select(wi => wi.ItemId)
                .Distinct()
                .ToListAsync(cancellationToken)).ToHashSet();
        }

        var rows = new List<AdminCompletedAuctionListItemDto>(candidateAuctions.Count);
        foreach (var auction in candidateAuctions)
        {
            if (!ordersByAuction.TryGetValue(auction.Id, out var order)) continue;

            var hasWarehouseItem = auction.Item is not null && warehouseItemItemIds.Contains(auction.Item.Id.Value);
            var flow = hasWarehouseItem ? "warehouse_managed" : "seller_self_ship";

            var paymentStatus = DerivePaymentStatus(order, nowUtc);
            var fulfillmentStatus = DeriveFulfillmentStatus(order, flow);

            // Apply derived filters in-memory.
            if (!string.IsNullOrWhiteSpace(parameters.PaymentStatus) &&
                !string.Equals(parameters.PaymentStatus, paymentStatus, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.IsNullOrWhiteSpace(parameters.FulfillmentStatus) &&
                !string.Equals(parameters.FulfillmentStatus, fulfillmentStatus, StringComparison.OrdinalIgnoreCase))
                continue;
            if (parameters.OnlyOverdue == true && !order.IsShippingOverdue && fulfillmentStatus != "shipping_overdue")
                continue;

            var buyer = usersById.TryGetValue(order.BuyerId.Value, out var b) ? b : null;
            var seller = usersById.TryGetValue(order.SellerId.Value, out var s) ? s : null;

            var primaryImageUrl = auction.Item?.Media
                .Where(m => m.IsPrimary)
                .OrderBy(m => m.SortOrder)
                .Select(m => m.Info.SecureUrl)
                .FirstOrDefault();

            rows.Add(new AdminCompletedAuctionListItemDto(
                AuctionId: auction.Id.Value,
                ItemTitle: auction.Item?.Title.Value ?? string.Empty,
                ItemPrimaryImageUrl: primaryImageUrl,
                WinnerId: auction.WinnerId?.Value,
                WinnerDisplayName: ResolveUserDisplayName(buyer),
                SellerId: order.SellerId.Value,
                SellerDisplayName: ResolveSellerDisplayName(seller),
                FinalPrice: order.Pricing.ItemPrice.Amount,
                Currency: order.Currency,
                OrderId: order.Id.Value,
                OrderNumber: order.OrderNumber.Value,
                OrderStatus: order.Status.Id,
                PaymentStatus: paymentStatus,
                FulfillmentFlow: flow,
                FulfillmentStatus: fulfillmentStatus,
                PaymentDueAt: order.PaymentDueAt,
                PaidAt: order.PaidAt,
                ShipByAt: order.ShipByAt,
                IsShippingOverdue: order.IsShippingOverdue,
                EscalatedAt: order.EscalatedAt,
                EscalationReason: order.EscalationReason,
                CreatedAt: order.CreatedAt));
        }

        var totalCount = rows.Count;
        var page = rows
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return new PagedList<AdminCompletedAuctionListItemDto>(page, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Precise derivation — see task spec:
    ///   pending_payment: order.Status == PendingPayment AND not past due
    ///   payment_overdue: order.Status == PendingPayment AND PaymentDueAt &lt; now
    ///   paid:            any post-payment state (Paid / Processing / shipped / delivered / completed / etc.)
    /// </summary>
    internal static string DerivePaymentStatus(Order order, DateTime nowUtc)
    {
        if (order.Status == OrderStatus.PendingPayment)
        {
            return order.PaymentDueAt is not null && order.PaymentDueAt < nowUtc
                ? "payment_overdue"
                : "pending_payment";
        }
        return "paid";
    }

    /// <summary>
    /// Fulfillment status precedence (highest wins):
    ///   1. shipping_overdue  → Order.IsShippingOverdue AND not yet delivered.
    ///   2. escalated         → Order.EscalatedAt set AND not yet delivered.
    ///   3. delivered         → order status delivered/completed OR active outbound shipment delivered.
    ///   4. on_delivering     → latest outbound shipment in InTransit (or order status on_delivering/shipped).
    ///   5. picked_up         → latest outbound shipment PickedUp (or order status picked_up).
    ///   6. awaiting_seller_ship     → pre-shipment, seller_self_ship flow.
    ///   7. warehouse_outbound_pending → pre-shipment, warehouse_managed flow.
    /// Matches the derivation used by the FE admin completed-auctions screen.
    /// </summary>
    internal static string DeriveFulfillmentStatus(Order order, string flow)
    {
        var isDelivered =
            order.Status == OrderStatus.Delivered ||
            order.Status == OrderStatus.Completed;

        if (order.IsShippingOverdue && !isDelivered) return "shipping_overdue";
        if (order.EscalatedAt is not null && !isDelivered) return "escalated";
        if (isDelivered) return "delivered";

        var latestShipment = order.OutboundShipments
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefault();

        if (latestShipment is not null)
        {
            var s = latestShipment.Status;
            if (s == Domain.Context.WarehouseContext.Enums.OutboundShipmentStatus.Delivered) return "delivered";
            if (s == Domain.Context.WarehouseContext.Enums.OutboundShipmentStatus.InTransit) return "on_delivering";
            if (s == Domain.Context.WarehouseContext.Enums.OutboundShipmentStatus.PickedUp) return "picked_up";
            // Pending / Booked fall through to pre-shipment states below.
        }

        if (order.Status == OrderStatus.OnDelivering || order.Status == OrderStatus.Shipped) return "on_delivering";
        if (order.Status == OrderStatus.PickedUp) return "picked_up";

        return flow == "warehouse_managed" ? "warehouse_outbound_pending" : "awaiting_seller_ship";
    }

    internal static string? ResolveUserDisplayName(User? user)
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

    internal static string? ResolveSellerDisplayName(User? user)
    {
        if (user is null) return null;
        if (user.SellerProfile is not null && !string.IsNullOrWhiteSpace(user.SellerProfile.StoreName))
            return user.SellerProfile.StoreName;
        return ResolveUserDisplayName(user);
    }
}
