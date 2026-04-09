using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.OutboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Queries.GetSellerWarehouseItems;

public record GetSellerWarehouseItemsQueryFilter : PagedParameters
{
    public string? WarehouseFlowStatus { get; init; }
    public string? Search { get; init; }
}

public sealed record GetSellerWarehouseItemsQuery(
    GetSellerWarehouseItemsQueryFilter Parameters)
    : IQuery<PagedList<SellerWarehouseItemListItemDto>>;

internal sealed class GetSellerWarehouseItemsQueryHandler(
    IDbContext db,
    ICurrentUser currentUser)
    : IQueryHandler<GetSellerWarehouseItemsQuery, PagedList<SellerWarehouseItemListItemDto>>
{
    public async Task<Result<PagedList<SellerWarehouseItemListItemDto>, Error>> Handle(
        GetSellerWarehouseItemsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var sellerId = currentUser.UserId;

        // Step 1: shipments owned by this seller (scope gate).
        var sellerShipments = await db.Set<InboundShipment>()
            .AsNoTracking()
            .Where(s => s.SellerId == sellerId)
            .ToListAsync(cancellationToken);

        if (sellerShipments.Count == 0)
            return PagedList<SellerWarehouseItemListItemDto>.Empty();

        var shipmentIds = sellerShipments.Select(s => s.Id).ToList();
        var shipmentsById = sellerShipments.ToDictionary(s => s.Id);

        // Step 2: warehouse items belonging to those shipments.
        var warehouseItems = await db.Set<WarehouseItem>()
            .AsNoTracking()
            .Include(w => w.Media)
            .Where(w => shipmentIds.Contains(w.InboundShipmentId))
            .ToListAsync(cancellationToken);

        if (warehouseItems.Count == 0)
            return PagedList<SellerWarehouseItemListItemDto>.Empty();

        // Step 3: batch enrich — items, storage locations, inspections, outbound.
        var itemIds = warehouseItems.Select(w => ItemId.From(w.ItemId)).Distinct().ToList();
        var items = await db.Set<Item>().AsNoTracking()
            .Include(i => i.Media)
            .Where(i => itemIds.Contains(i.Id))
            .ToListAsync(cancellationToken);
        var itemsById = items.ToDictionary(i => i.Id.Value);

        var locationIds = warehouseItems
            .Where(w => w.StorageLocationId != null)
            .Select(w => w.StorageLocationId!)
            .Distinct()
            .ToList();
        var locations = locationIds.Count == 0
            ? new List<WarehouseStorageLocation>()
            : await db.Set<WarehouseStorageLocation>().AsNoTracking()
                .Where(l => locationIds.Contains(l.Id))
                .ToListAsync(cancellationToken);
        var locationsById = locations.ToDictionary(l => l.Id);

        var warehouseItemIds = warehouseItems.Select(w => w.Id).ToList();
        var inspections = await db.Set<WarehouseInspection>().AsNoTracking()
            .Where(x => warehouseItemIds.Contains(x.WarehouseItemId))
            .ToListAsync(cancellationToken);
        var latestInspectionByItem = inspections
            .GroupBy(x => x.WarehouseItemId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.InspectedAt).First());

        var outbounds = await db.Set<OutboundShipment>().AsNoTracking()
            .Where(o => o.WarehouseItemId != null)
            .ToListAsync(cancellationToken);
        var latestOutboundByItem = new Dictionary<WarehouseItemId, OutboundShipment>();
        foreach (var o in outbounds)
        {
            if (o.WarehouseItemId is not { } key) continue;
            if (!warehouseItemIds.Contains(key)) continue;
            if (!latestOutboundByItem.TryGetValue(key, out var existing) || o.CreatedAt > existing.CreatedAt)
                latestOutboundByItem[key] = o;
        }

        // Step 4: project to DTO (flow status computed in-memory).
        var projected = warehouseItems
            .Select(w =>
            {
                itemsById.TryGetValue(w.ItemId, out var item);
                shipmentsById.TryGetValue(w.InboundShipmentId, out var shipment);

                string? locationLabel = null;
                if (w.StorageLocationId is { } locId &&
                    locationsById.TryGetValue(locId, out var loc))
                {
                    locationLabel = loc.Label;
                }

                latestInspectionByItem.TryGetValue(w.Id, out var inspection);
                latestOutboundByItem.TryGetValue(w.Id, out var outbound);

                var primaryMedia = item?.Media.FirstOrDefault(m => m.IsPrimary)
                                   ?? item?.Media.FirstOrDefault();

                var flowStatus = SellerWarehouseFlowStatusResolver.Resolve(w, inspection, outbound);

                return new SellerWarehouseItemListItemDto(
                    WarehouseItemId:       w.Id.Value,
                    ItemId:                w.ItemId,
                    ItemTitle:             item?.Title.Value,
                    ItemImageUrl:          primaryMedia?.Info.SecureUrl,
                    InboundPackageCode:    shipment?.ClientOrderCode,
                    InboundShipmentId:     w.InboundShipmentId.Value,
                    StorageLocationLabel:  locationLabel,
                    ReceivedAt:            w.ReceivedAt,
                    UpdatedAt:             w.ModifiedAt ?? w.CreatedAt,
                    WarehouseFlowStatus:   flowStatus,
                    WarehouseItemStatusRaw: w.Status.Id);
            })
            .ToList();

        // Step 5: post-enrichment filters.
        if (!string.IsNullOrWhiteSpace(parameters.WarehouseFlowStatus))
        {
            var wanted = parameters.WarehouseFlowStatus.Trim().ToLowerInvariant();
            projected = projected.Where(x => x.WarehouseFlowStatus == wanted).ToList();
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var s = parameters.Search.Trim().ToLowerInvariant();
            projected = projected.Where(x =>
                (x.ItemTitle != null && x.ItemTitle.ToLowerInvariant().Contains(s)) ||
                (x.InboundPackageCode != null && x.InboundPackageCode.ToLowerInvariant().Contains(s)) ||
                (x.StorageLocationLabel != null && x.StorageLocationLabel.ToLowerInvariant().Contains(s)))
                .ToList();
        }

        var ordered = projected
            .OrderByDescending(x => x.UpdatedAt)
            .ToList();

        var totalCount = ordered.Count;
        var pageNumber = parameters.EffectivePageNumber;
        var pageSize = parameters.EffectivePageSize;

        var pagedItems = ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedList<SellerWarehouseItemListItemDto>(
            pagedItems, totalCount, pageNumber, pageSize);
    }
}
