using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.Context.WarehouseContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Queries.GetWarehouseItems;

public sealed record GetWarehouseItemsQuery(GetWarehouseItemsQueryFilter Parameters)
    : IQuery<PagedList<WarehouseItemDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetWarehouseItemsQuery.Check()
            .WithOwnerName("GetWarehouseItems")
            .Field(Parameters.Status)
            .WhenHasValue(x => x.InSet(WarehouseItemStatus.All.Select(s => s.Id)));
    }
}

internal sealed class GetWarehouseItemsQueryHandler(IDbContext db)
    : IQueryHandler<GetWarehouseItemsQuery, PagedList<WarehouseItemDto>>
{
    public async Task<Result<PagedList<WarehouseItemDto>, Error>> Handle(
        GetWarehouseItemsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        // ── Base query: EF-translatable filters on WarehouseItem only ─────
        var baseQuery = db.Set<WarehouseItem>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = WarehouseItemStatus.FromId(parameters.Status.Trim().ToLowerInvariant());
            if (status.HasValue)
                baseQuery = baseQuery.Where(w => w.Status == status.Value);
        }

        if (parameters.StorageLocationId.HasValue)
        {
            var locId = WarehouseStorageLocationId.From(parameters.StorageLocationId.Value);
            baseQuery = baseQuery.Where(w => w.StorageLocationId == locId);
        }

        if (parameters.ItemId.HasValue)
        {
            var itemIdValue = parameters.ItemId.Value;
            baseQuery = baseQuery.Where(w => w.ItemId == itemIdValue);
        }

        if (parameters.InboundShipmentId.HasValue)
        {
            var shipmentId = InboundShipmentId.From(parameters.InboundShipmentId.Value);
            baseQuery = baseQuery.Where(w => w.InboundShipmentId == shipmentId);
        }

        var warehouseItems = await baseQuery
            .Include(w => w.Media)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(cancellationToken);

        if (warehouseItems.Count == 0)
            return PagedList<WarehouseItemDto>.Empty();

        // ── Batch-load related entities in memory ────────────────────────
        var shipmentIds = warehouseItems.Select(w => w.InboundShipmentId).Distinct().ToList();
        var shipments = await db.Set<InboundShipment>().AsNoTracking()
            .Where(s => shipmentIds.Contains(s.Id))
            .ToListAsync(cancellationToken);
        var shipmentsById = shipments.ToDictionary(s => s.Id.Value);

        var itemIds = warehouseItems.Select(w => ItemId.From(w.ItemId)).Distinct().ToList();
        var items = await db.Set<Item>().AsNoTracking()
            .Include(i => i.Media)
            .Where(i => itemIds.Contains(i.Id))
            .ToListAsync(cancellationToken);
        var itemsById = items.ToDictionary(i => i.Id.Value);

        var sellerIds = shipments.Select(s => s.SellerId).Distinct().ToList();
        var sellers = await db.Set<User>().AsNoTracking()
            .Include(u => u.Profile)
            .Where(u => sellerIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
        var sellersById = sellers.ToDictionary(u => u.Id.Value);

        var locationIds = warehouseItems
            .Where(w => w.StorageLocationId != null)
            .Select(w => w.StorageLocationId!)
            .Distinct()
            .ToList();
        var locations = await db.Set<WarehouseStorageLocation>().AsNoTracking()
            .Where(l => locationIds.Contains(l.Id))
            .ToListAsync(cancellationToken);
        var locationsById = locations.ToDictionary(l => l.Id.Value);

        // ── In-memory projection to DTO ──────────────────────────────────
        var projected = warehouseItems
            .Select(w =>
            {
                itemsById.TryGetValue(w.ItemId, out var item);
                shipmentsById.TryGetValue(w.InboundShipmentId.Value, out var shipment);
                User? seller = null;
                if (shipment != null)
                    sellersById.TryGetValue(shipment.SellerId.Value, out seller);

                string? locationLabel = null;
                Guid? locationId = null;
                if (w.StorageLocationId is { } locId &&
                    locationsById.TryGetValue(locId.Value, out var loc))
                {
                    locationLabel = loc.Label;
                    locationId = locId.Value;
                }

                var primaryMedia = item?.Media.FirstOrDefault(m => m.IsPrimary)
                                   ?? item?.Media.FirstOrDefault();

                return new WarehouseItemDto(
                    Id:                   w.Id.Value,
                    ItemId:               w.ItemId,
                    InboundShipmentId:    w.InboundShipmentId.Value,
                    InboundShipmentCode:  shipment?.ClientOrderCode,
                    StorageLocationId:    locationId,
                    StorageLocationLabel: locationLabel,
                    ItemTitle:            item?.Title.Value,
                    SellerId:             shipment?.SellerId.Value,
                    SellerName:           seller?.Profile?.Name.DisplayName ?? seller?.UserName.Value,
                    ItemImageUrl:         primaryMedia?.Info.SecureUrl,
                    Status:               w.Status.Id,
                    ReceivedAt:           w.ReceivedAt,
                    CreatedAt:            w.CreatedAt,
                    ModifiedAt:           w.ModifiedAt,
                    Media:                w.Media
                        .OrderBy(m => m.SortOrder)
                        .Select(m => new WarehouseItemMediaDto(
                            Id:           m.Id.Value,
                            ResourceType: m.ResourceType,
                            IsPrimary:    m.IsPrimary,
                            SortOrder:    m.SortOrder,
                            SecureUrl:    m.Info.SecureUrl,
                            FileName:     m.Info.FileName))
                        .ToList());
            })
            .ToList();

        // ── Post-enrichment filters: sellerId + searchTerm ───────────────
        if (parameters.SellerId.HasValue)
        {
            var sid = parameters.SellerId.Value;
            projected = projected.Where(x => x.SellerId == sid).ToList();
        }

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var search = parameters.SearchTerm.Trim().ToLowerInvariant();
            var sellerUserNameById = sellers.ToDictionary(
                u => u.Id.Value,
                u => u.UserName.Value.ToLowerInvariant());

            projected = projected.Where(x =>
                (x.ItemTitle != null && x.ItemTitle.ToLowerInvariant().Contains(search)) ||
                (x.InboundShipmentCode != null && x.InboundShipmentCode.ToLowerInvariant().Contains(search)) ||
                (x.StorageLocationLabel != null && x.StorageLocationLabel.ToLowerInvariant().Contains(search)) ||
                (x.SellerName != null && x.SellerName.ToLowerInvariant().Contains(search)) ||
                (x.SellerId.HasValue && sellerUserNameById.TryGetValue(x.SellerId.Value, out var un) && un.Contains(search)))
                .ToList();
        }

        // ── Order + paginate ─────────────────────────────────────────────
        var ordered = projected
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        var totalCount = ordered.Count;
        var pageNumber = parameters.EffectivePageNumber;
        var pageSize = parameters.EffectivePageSize;

        var pagedItems = ordered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedList<WarehouseItemDto>(pagedItems, totalCount, pageNumber, pageSize);
    }
}
