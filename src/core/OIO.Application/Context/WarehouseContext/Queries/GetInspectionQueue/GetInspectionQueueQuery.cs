using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetInspectionQueue;

public sealed record GetInspectionQueueQuery(
    int Page = 1,
    int PageSize = 20) : IQuery<IReadOnlyList<InspectionQueueItemDto>>;

internal sealed class GetInspectionQueueQueryHandler(Application.Abstractions.Data.IDbContext db)
    : IQueryHandler<GetInspectionQueueQuery, IReadOnlyList<InspectionQueueItemDto>>
{
    public async Task<Result<IReadOnlyList<InspectionQueueItemDto>, Error>> Handle(
        GetInspectionQueueQuery request,
        CancellationToken cancellationToken)
    {
        var shipments = await db.Set<InboundShipment>()
            .AsNoTracking()
            .Where(x =>
                x.Status == InboundShipmentStatus.Arrived ||
                x.Status == InboundShipmentStatus.Inspected)
            .OrderByDescending(x => x.ArrivedAt ?? x.CreatedAt)
            .ToListAsync(cancellationToken);

        if (shipments.Count == 0)
            return Array.Empty<InspectionQueueItemDto>();

        var shipmentIds = shipments.Select(x => x.Id).ToList();
        var itemIds = shipments.Select(x => x.ItemId).Distinct().ToList();

        var inspections = await db.Set<WarehouseInspection>()
            .AsNoTracking()
            .Where(x => shipmentIds.Contains(x.InboundShipmentId))
            .ToListAsync(cancellationToken);

        var items = await db.Set<Item>()
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.Id.Value))
            .ToListAsync(cancellationToken);

        var queue = shipments
            .Select(shipment =>
            {
                var inspection = inspections.FirstOrDefault(x => x.InboundShipmentId == shipment.Id);
                var item = items.FirstOrDefault(x => x.Id.Value == shipment.ItemId);
                if (item is null)
                    return null;

                var queueStatus = inspection is null
                    ? "awaiting_inspection"
                    : inspection.DecisionStatus == WarehouseInspectionDecisionStatus.PendingReview
                        ? "awaiting_review"
                        : null;

                if (queueStatus is null)
                    return null;

                if (shipment.Status == InboundShipmentStatus.Arrived && inspection is not null)
                    return null;

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
                    QueueStatus: queueStatus,
                    CarrierTrackingNumber: shipment.CarrierTrackingNumber,
                    ArrivedAt: shipment.ArrivedAt,
                    DeclaredCondition: inspection?.DeclaredCondition.Id ?? item.Condition.Id,
                    ConditionOnArrival: inspection?.ConditionOnArrival.Id,
                    InspectedAt: inspection?.InspectedAt);
            })
            .Where(x => x is not null)
            .Cast<InspectionQueueItemDto>()
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return queue;
    }
}
