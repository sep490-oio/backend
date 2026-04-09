using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseStorage;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Queries.GetInspectionQueue;

public record GetInspectionQueueQueryFilters : PagedParameters
{
    /// <summary>
    /// Optional filter by queue status: "awaiting_inspection" or "awaiting_review".
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Optional filter: only include items whose Item.RequiresPlatformInspection matches.
    /// </summary>
    public bool? RequiresPlatformInspection { get; init; }
}

public sealed record GetInspectionQueueQuery(
    GetInspectionQueueQueryFilters Parameters) : IQuery<PagedList<InspectionQueueItemDto>>;

internal sealed class GetInspectionQueueQueryHandler(Application.Abstractions.Data.IDbContext db)
    : IQueryHandler<GetInspectionQueueQuery, PagedList<InspectionQueueItemDto>>
{
    public async Task<Result<PagedList<InspectionQueueItemDto>, Error>> Handle(
        GetInspectionQueueQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        // Source of truth: WarehouseItem rows. Inspector queue is independent of the
        // inbound shipment's terminal status — once the item has been received, it's a
        // candidate for inspection regardless of whether the shipment is Arrived or Completed.
        // Happy path: only items that have been placed into a storage location.
        // Include Inspected for backward compatibility (re-review / pending review).
        var warehouseItems = await db.Set<WarehouseItem>()
            .AsNoTracking()
            .Where(w => w.Status == WarehouseItemStatus.Stored ||
                        w.Status == WarehouseItemStatus.Inspected)
            .ToListAsync(cancellationToken);

        if (warehouseItems.Count == 0)
            return PagedList<InspectionQueueItemDto>.Empty();

        var itemIds = warehouseItems.Select(w => ItemId.From(w.ItemId)).Distinct().ToList();

        var itemsQuery = db.Set<Item>().AsNoTracking().Include(x => x.Media).Where(x => itemIds.Contains(x.Id));
        if (parameters.RequiresPlatformInspection.HasValue)
        {
            var flag = parameters.RequiresPlatformInspection.Value;
            itemsQuery = itemsQuery.Where(x => x.RequiresPlatformInspection == flag);
        }

        var items = await itemsQuery.ToListAsync(cancellationToken);
        var itemsById = items.ToDictionary(x => x.Id.Value);

        var warehouseItemIds = warehouseItems.Select(w => w.Id).ToList();
        var inspections = await db.Set<WarehouseInspection>()
            .AsNoTracking()
            .Where(x => warehouseItemIds.Contains(x.WarehouseItemId))
            .ToListAsync(cancellationToken);

        var shipmentIds = warehouseItems.Select(w => w.InboundShipmentId).Distinct().ToList();
        var shipments = await db.Set<InboundShipment>()
            .AsNoTracking()
            .Where(s => shipmentIds.Contains(s.Id))
            .ToListAsync(cancellationToken);
        var shipmentsById = shipments.ToDictionary(s => s.Id);

        var locationIds = warehouseItems
            .Where(w => w.StorageLocationId != null)
            .Select(w => w.StorageLocationId!)
            .Distinct()
            .ToList();
        var locations = await db.Set<WarehouseStorageLocation>()
            .AsNoTracking()
            .Where(l => locationIds.Contains(l.Id))
            .ToListAsync(cancellationToken);
        var locationsById = locations.ToDictionary(l => l.Id);

        var allItems = warehouseItems
            .Select(wi =>
            {
                if (!itemsById.TryGetValue(wi.ItemId, out var item))
                    return null;

                var inspection = inspections.FirstOrDefault(x => x.WarehouseItemId == wi.Id);

                string? queueStatusId;
                if (inspection is null)
                {
                    queueStatusId = InspectionQueueStatus.AwaitingInspection.Id;
                }
                else if (inspection.DecisionStatus == WarehouseInspectionDecisionStatus.PendingReview)
                {
                    queueStatusId = InspectionQueueStatus.AwaitingReview.Id;
                }
                else
                {
                    return null;
                }

                shipmentsById.TryGetValue(wi.InboundShipmentId, out var shipment);
                string? locationLabel = null;
                if (wi.StorageLocationId is { } locId &&
                    locationsById.TryGetValue(locId, out var loc))
                {
                    locationLabel = loc.Label;
                }

                var primaryMedia = item.Media.FirstOrDefault(m => m.IsPrimary)
                                   ?? item.Media.FirstOrDefault();

                return new InspectionQueueItemDto(
                    InboundShipmentId: wi.InboundShipmentId.Value,
                    ItemId: item.Id.Value,
                    ItemTitle: item.Title.Value,
                    SellerId: item.SellerId.Value,
                    WarehouseItemId: wi.Id.Value,
                    InspectionId: inspection?.Id.Value,
                    ShipmentStatus: shipment?.Status.Id ?? string.Empty,
                    QueueStatus: queueStatusId,
                    CarrierTrackingNumber: shipment?.CarrierTrackingNumber,
                    ArrivedAt: shipment?.ArrivedAt ?? wi.ReceivedAt,
                    DeclaredCondition: inspection?.DeclaredCondition.Id ?? item.Condition.Id,
                    ConditionOnArrival: inspection?.ConditionOnArrival.Id,
                    InspectedAt: inspection?.InspectedAt,
                    StorageLocationLabel: locationLabel,
                    ItemImageUrl: primaryMedia?.Info.SecureUrl);
            })
            .Where(x => x is not null)
            .Cast<InspectionQueueItemDto>()
            .OrderByDescending(x => x.ArrivedAt)
            .ToList();

        // Apply optional status filter
        if (!string.IsNullOrEmpty(parameters.Status))
        {
            allItems = allItems
                .Where(x => x.QueueStatus == parameters.Status)
                .ToList();
        }

        // Apply pagination
        var totalCount = allItems.Count;
        var pageNumber = parameters.EffectivePageNumber;
        var pageSize = parameters.EffectivePageSize;

        var pagedItems = allItems
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedList<InspectionQueueItemDto>(
            pagedItems, totalCount, pageNumber, pageSize);
    }
}
