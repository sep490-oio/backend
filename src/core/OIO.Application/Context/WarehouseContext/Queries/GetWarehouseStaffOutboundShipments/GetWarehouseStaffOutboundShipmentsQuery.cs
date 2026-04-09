using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetWarehouseStaffOutboundShipments;

/// <summary>
/// Paginated list of <see cref="OutboundShipment"/> rows for the warehouse-staff
/// "Booked Shipments" tab. Optional filters by status, shipment mode, and a free
/// text search that matches either the carrier tracking number or the order number.
/// </summary>
public sealed record GetWarehouseStaffOutboundShipmentsQuery(
    PagedParameters Parameters,
    string? Status = null,
    string? ShipmentMode = null,
    string? Search = null)
    : IQuery<PagedList<WarehouseStaffOutboundShipmentListItemDto>>;

internal sealed class GetWarehouseStaffOutboundShipmentsQueryHandler(IDbContext db)
    : IQueryHandler<GetWarehouseStaffOutboundShipmentsQuery, PagedList<WarehouseStaffOutboundShipmentListItemDto>>
{
    public async Task<Result<PagedList<WarehouseStaffOutboundShipmentListItemDto>, Error>> Handle(
        GetWarehouseStaffOutboundShipmentsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        var q = db.Set<OutboundShipment>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var statusVo = OutboundShipmentStatus.FromId(request.Status);
            if (statusVo.HasValue)
                q = q.Where(s => s.Status == statusVo.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ShipmentMode))
        {
            var modeVo = OutboundShipmentMode.FromId(request.ShipmentMode);
            if (modeVo.HasValue)
                q = q.Where(s => s.ShipmentMode == modeVo.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            q = q.Where(s =>
                (s.CarrierTrackingNumber != null && s.CarrierTrackingNumber.Contains(term)) ||
                s.ClientOrderCode.Contains(term));
        }

        var totalCount = await q.CountAsync(cancellationToken);

        var pagedShipments = await q
            .OrderByDescending(s => s.CreatedAt)
            .Page(parameters)
            .ToListAsync(cancellationToken);

        if (pagedShipments.Count == 0)
            return new List<WarehouseStaffOutboundShipmentListItemDto>().ToPagedList(totalCount, parameters);

        // Batch-enrich via separate queries — never cross VO-id boundaries inside a
        // single EF query.
        var orderIds = pagedShipments.Select(s => s.OrderId).Distinct().ToList();
        var orders = await db.Set<Order>()
            .AsNoTracking()
            .Where(o => orderIds.Contains(o.Id))
            .ToListAsync(cancellationToken);
        var ordersById = orders.ToDictionary(o => o.Id);

        // Apply search on order number as a secondary filter (post-load) since
        // OrderNumber is a VO and can't easily be pushed through Contains on a
        // predicate over the OutboundShipment query.
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            var matchedOrderIds = orders
                .Where(o => o.OrderNumber.Value.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Select(o => o.Id)
                .ToHashSet();
            var filtered = pagedShipments
                .Where(s => s.CarrierTrackingNumber?.Contains(term, StringComparison.OrdinalIgnoreCase) == true
                    || s.ClientOrderCode.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || matchedOrderIds.Contains(s.OrderId))
                .ToList();
            pagedShipments = filtered;
        }

        var auctionIds = orders.Select(o => o.AuctionId).Distinct().ToList();
        var auctions = await db.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Item)
                .ThenInclude(i => i.Media)
            .Where(a => auctionIds.Contains(a.Id))
            .ToListAsync(cancellationToken);
        var auctionsById = auctions.ToDictionary(a => a.Id);

        var warehouseItemIds = pagedShipments
            .Where(s => s.WarehouseItemId is not null)
            .Select(s => s.WarehouseItemId!)
            .Distinct()
            .ToList();
        var warehouseItems = warehouseItemIds.Count == 0
            ? new List<WarehouseItem>()
            : await db.Set<WarehouseItem>()
                .AsNoTracking()
                .Where(wi => warehouseItemIds.Contains(wi.Id))
                .ToListAsync(cancellationToken);
        var warehouseItemsById = warehouseItems.ToDictionary(wi => wi.Id);

        var locationIds = warehouseItems
            .Where(wi => wi.StorageLocationId is not null)
            .Select(wi => wi.StorageLocationId!)
            .Distinct()
            .ToList();
        var locations = locationIds.Count == 0
            ? new List<WarehouseStorageLocation>()
            : await db.Set<WarehouseStorageLocation>()
                .AsNoTracking()
                .Where(l => locationIds.Contains(l.Id))
                .ToListAsync(cancellationToken);
        var locationLabelById = locations.ToDictionary(l => l.Id, l => l.Label);

        var rows = pagedShipments.Select(s =>
        {
            ordersById.TryGetValue(s.OrderId, out var order);
            var auction = order is not null && auctionsById.TryGetValue(order.AuctionId, out var a) ? a : null;
            var item = auction?.Item;
            var primaryImageUrl = item?.Media
                .Where(m => m.IsPrimary)
                .OrderBy(m => m.SortOrder)
                .Select(m => m.Info.SecureUrl)
                .FirstOrDefault();

            string? storageLocationLabel = null;
            if (s.WarehouseItemId is { } whId &&
                warehouseItemsById.TryGetValue(whId, out var wi) &&
                wi.StorageLocationId is { } locId &&
                locationLabelById.TryGetValue(locId, out var label))
            {
                storageLocationLabel = label;
            }

            return new WarehouseStaffOutboundShipmentListItemDto(
                ShipmentId: s.Id.Value,
                OrderId: s.OrderId.Value,
                OrderNumber: order?.OrderNumber.Value ?? string.Empty,
                Status: s.Status.Id,
                ShipmentMode: s.ShipmentMode.Id,
                ProviderCode: s.ProviderCode.Id,
                ExternalCarrierName: s.ExternalCarrierName,
                CarrierTrackingNumber: s.CarrierTrackingNumber,
                ItemTitle: item?.Title.Value,
                ItemPrimaryImageUrl: primaryImageUrl,
                StorageLocationLabel: storageLocationLabel,
                RecipientName: order?.Shipping?.RecipientName,
                CreatedAt: s.CreatedAt,
                DispatchedAt: s.DispatchedAt);
        }).ToList();

        return rows.ToPagedList(totalCount, parameters);
    }
}
