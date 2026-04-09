using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetWarehouseStaffOutboundQueue;

/// <summary>
/// Warehouse-staff queue of orders waiting to have an outbound shipment booked.
/// An order qualifies when:
///   - its status is <see cref="OrderStatus.Paid"/> or <see cref="OrderStatus.Processing"/>,
///   - its item has a <see cref="WarehouseItem"/> row (i.e., it is warehouse-managed —
///     same ownership rule as <c>OrderMappings.BuildSellerFulfillment</c>), and
///   - it has no active <c>OutboundShipment</c> (no shipment at all, or every shipment
///     is in a terminal state: Cancelled, Failed, Returned, or Delivered).
/// </summary>
public sealed record GetWarehouseStaffOutboundQueueQuery(PagedParameters Parameters)
    : IQuery<PagedList<WarehouseStaffOutboundQueueItemDto>>;

internal sealed class GetWarehouseStaffOutboundQueueQueryHandler(IDbContext dbContext)
    : IQueryHandler<GetWarehouseStaffOutboundQueueQuery, PagedList<WarehouseStaffOutboundQueueItemDto>>
{
    public async Task<Result<PagedList<WarehouseStaffOutboundQueueItemDto>, Error>> Handle(
        GetWarehouseStaffOutboundQueueQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        // Base filter: Processing orders only. Paid orders haven't been confirmed by
        // the seller yet; only once they transition to Processing do they become
        // actionable for the warehouse outbound booking workflow.
        var ordersQuery = dbContext.Set<Order>()
            .Where(o => o.Status == OrderStatus.Processing);

        // Narrow to warehouse-managed orders by intersecting against the set of
        // auction item ids that have at least one WarehouseItem row.
        // EF cannot translate nested subqueries that cross the WarehouseItem.ItemId (raw Guid)
        // vs Auction.Item.Id.Value (value-object id) boundary — so materialize each step.
        var whRawItemGuids = await dbContext.Set<WarehouseItem>()
            .Select(wi => wi.ItemId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var whItemIds = whRawItemGuids
            .Select(g => OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId.From(g))
            .ToList();

        var warehouseManagedAuctionIds = await dbContext.Set<Auction>()
            .Where(a => whItemIds.Contains(a.ItemId))
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        ordersQuery = ordersQuery.Where(o => warehouseManagedAuctionIds.Contains(o.AuctionId));

        // Exclude orders that already have an *active* outbound shipment. An order
        // with zero OutboundShipments still qualifies; an order whose shipments are
        // all in terminal states (Cancelled / Failed / Returned / Delivered) also
        // qualifies — staff may need to rebook after a cancellation/failure.
        ordersQuery = ordersQuery.Where(o => !o.OutboundShipments.Any(s =>
            s.Status != OutboundShipmentStatus.Cancelled &&
            s.Status != OutboundShipmentStatus.Failed &&
            s.Status != OutboundShipmentStatus.Returned &&
            s.Status != OutboundShipmentStatus.Delivered));

        var totalCount = await ordersQuery.CountAsync(cancellationToken);

        var pagedOrders = await ordersQuery
            .Include(o => o.OutboundShipments)
            .OrderBy(o => o.PaidAt ?? o.CreatedAt)
            .Page(parameters)
            .ToListAsync(cancellationToken);

        if (pagedOrders.Count == 0)
        {
            return new List<WarehouseStaffOutboundQueueItemDto>()
                .ToPagedList(totalCount, parameters);
        }

        // Batch-load auctions (with item + media) for the paged orders.
        var auctionIds = pagedOrders.Select(o => o.AuctionId).Distinct().ToList();
        var auctions = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
                .ThenInclude(i => i.Media)
            .Where(a => auctionIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
        var auctionsById = auctions.ToDictionary(a => a.Id);

        // Batch-load warehouse items for these auction items so we can surface
        // the WarehouseItemId on each row (used by the FE to open the booking UI).
        // Tighten to shippable statuses — the happy path is Stored, but legacy
        // rows may still be in Received / Inspected (WarehouseItem.Reserve accepts
        // all three).
        var shippable = new[]
        {
            WarehouseItemStatus.Stored,
            WarehouseItemStatus.Received,
            WarehouseItemStatus.Inspected,
        };
        var itemGuidIds = auctions.Select(a => a.Item.Id.Value).Distinct().ToList();
        var warehouseItems = await dbContext.Set<WarehouseItem>()
            .AsNoTracking()
            .Where(wi => itemGuidIds.Contains(wi.ItemId) && shippable.Contains(wi.Status))
            .ToListAsync(cancellationToken);
        var warehouseItemByItemId = warehouseItems
            .GroupBy(wi => wi.ItemId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(wi => wi.CreatedAt).First());

        // Batch-load storage location labels for the picked warehouse items.
        var locationIds = warehouseItems
            .Where(wi => wi.StorageLocationId is not null)
            .Select(wi => wi.StorageLocationId!)
            .Distinct()
            .ToList();
        var locations = locationIds.Count == 0
            ? new List<WarehouseStorageLocation>()
            : await dbContext.Set<WarehouseStorageLocation>()
                .AsNoTracking()
                .Where(l => locationIds.Contains(l.Id))
                .ToListAsync(cancellationToken);
        var locationLabelById = locations.ToDictionary(l => l.Id, l => l.Label);

        // Batch-load sellers for seller display name resolution (store name preferred).
        var sellerIds = pagedOrders.Select(o => o.SellerId).Distinct().ToList();
        var sellers = await dbContext.Set<User>()
            .AsNoTracking()
            .Include(u => u.SellerProfile)
            .Where(u => sellerIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
        var sellersById = sellers.ToDictionary(u => u.Id);

        var rows = pagedOrders
            .Select(o =>
            {
                auctionsById.TryGetValue(o.AuctionId, out var auction);
                var item = auction?.Item;

                var primaryImageUrl = item?.Media
                    .Where(m => m.IsPrimary)
                    .OrderBy(m => m.SortOrder)
                    .Select(m => m.Info.SecureUrl)
                    .FirstOrDefault();

                Guid warehouseItemId = Guid.Empty;
                string? storageLocationLabel = null;
                if (item is not null &&
                    warehouseItemByItemId.TryGetValue(item.Id.Value, out var wi))
                {
                    warehouseItemId = wi.Id.Value;
                    if (wi.StorageLocationId is { } locId &&
                        locationLabelById.TryGetValue(locId, out var label))
                    {
                        storageLocationLabel = label;
                    }
                }

                sellersById.TryGetValue(o.SellerId, out var seller);
                var sellerDisplayName = ResolveSellerDisplayName(seller);

                string? shippingSummary = null;
                if (o.Shipping is not null)
                {
                    shippingSummary = o.Shipping.IsStructured
                        ? string.Join(", ", new[]
                            {
                                o.Shipping.Street,
                                o.Shipping.Ward,
                                o.Shipping.District,
                                o.Shipping.City,
                                o.Shipping.PostalCode
                            }.Where(p => !string.IsNullOrWhiteSpace(p)))
                        : o.Shipping.Address;
                }

                return new WarehouseStaffOutboundQueueItemDto(
                    OrderId: o.Id.Value,
                    OrderNumber: o.OrderNumber.Value,
                    OrderStatus: o.Status.Id,
                    OrderPaidAt: o.PaidAt,
                    AuctionId: o.AuctionId.Value,
                    WarehouseItemId: warehouseItemId,
                    ItemTitle: item?.Title.Value,
                    ItemPrimaryImageUrl: primaryImageUrl,
                    BuyerRecipientName: o.Shipping?.RecipientName,
                    BuyerShippingAddress: shippingSummary,
                    SellerDisplayName: sellerDisplayName,
                    StorageLocationLabel: storageLocationLabel);
            })
            // Drop any orders whose WarehouseItem lookup failed after the narrowing
            // subquery (should be rare; guards against race with dispatch cleanup).
            .Where(r => r.WarehouseItemId != Guid.Empty)
            .ToList();

        return rows.ToPagedList(totalCount, parameters);
    }

    private static string? ResolveSellerDisplayName(User? user)
    {
        if (user is null) return null;
        if (user.SellerProfile is not null && !string.IsNullOrWhiteSpace(user.SellerProfile.StoreName))
            return user.SellerProfile.StoreName;
        var profile = user.Profile;
        if (profile?.Name is not null)
        {
            if (!string.IsNullOrWhiteSpace(profile.Name.DisplayName)) return profile.Name.DisplayName;
            if (!string.IsNullOrWhiteSpace(profile.Name.FullName)) return profile.Name.FullName;
        }
        return user.UserName?.Value;
    }
}
