using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
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

        var shipments = await db.Set<InboundShipment>()
            .AsNoTracking()
            .Where(x =>
                x.Status == InboundShipmentStatus.Arrived ||
                x.Status == InboundShipmentStatus.Inspected)
            .OrderByDescending(x => x.ArrivedAt ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

        if (shipments.Count == 0)
            return PagedList<InspectionQueueItemDto>.Empty();

        var shipmentIds = shipments.Select(x => x.Id).ToList();
        var itemIds = shipments.Select(x => ItemId.From(x.ItemId)).Distinct().ToList();

        var inspections = await db.Set<WarehouseInspection>()
            .AsNoTracking()
            .Where(x => shipmentIds.Contains(x.InboundShipmentId))
            .ToListAsync(cancellationToken);

        var items = await db.Set<Item>()
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var itemsById = items.ToDictionary(x => x.Id.Value);

        var allItems = shipments
            .Select(shipment =>
            {
                var inspection = inspections.FirstOrDefault(x => x.InboundShipmentId == shipment.Id);
                if (!itemsById.TryGetValue(shipment.ItemId, out var item))
                    return null;

                var queueStatus = inspection is null
                    ? InspectionQueueStatus.AwaitingInspection
                    : inspection.DecisionStatus == WarehouseInspectionDecisionStatus.PendingReview
                        ? InspectionQueueStatus.AwaitingReview
                        : null;

                if (queueStatus is null)
                    return null;

                // Arrived + already inspected = skip (shouldn't happen but guard)
                if (shipment.Status == InboundShipmentStatus.Arrived && inspection is not null)
                    return null;

                // Inspected + already reviewed = skip
                if (shipment.Status == InboundShipmentStatus.Inspected &&
                    inspection?.DecisionStatus != WarehouseInspectionDecisionStatus.PendingReview)
                    return null;

                return new InspectionQueueItemDto(
                    InboundShipmentId: shipment.Id.Value,
                    ItemId: item.Id.Value,
                    ItemTitle: item.Title.Value,
                    SellerId: item.SellerId.Value,
                    WarehouseItemId: inspection?.WarehouseItemId.Value,
                    InspectionId: inspection?.Id.Value,
                    ShipmentStatus: shipment.Status.Id,
                    QueueStatus: queueStatus.Id,
                    CarrierTrackingNumber: shipment.CarrierTrackingNumber,
                    ArrivedAt: shipment.ArrivedAt,
                    DeclaredCondition: inspection?.DeclaredCondition.Id ?? item.Condition.Id,
                    ConditionOnArrival: inspection?.ConditionOnArrival.Id,
                    InspectedAt: inspection?.InspectedAt);
            })
            .Where(x => x is not null)
            .Cast<InspectionQueueItemDto>()
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
