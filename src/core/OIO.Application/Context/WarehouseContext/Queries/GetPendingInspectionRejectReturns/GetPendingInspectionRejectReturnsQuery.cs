using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Extensions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.WarehouseContext.Queries.GetPendingInspectionRejectReturns;

public sealed record GetPendingInspectionRejectReturnsQuery(PagedParameters Parameters)
    : IQuery<PagedList<PendingInspectionRejectReturnDto>>;

internal sealed class GetPendingInspectionRejectReturnsQueryHandler(IDbContext dbContext)
    : IQueryHandler<GetPendingInspectionRejectReturnsQuery, PagedList<PendingInspectionRejectReturnDto>>
{
    public async Task<Result<PagedList<PendingInspectionRejectReturnDto>, Error>> Handle(
        GetPendingInspectionRejectReturnsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        var query = dbContext.Set<WarehouseInspection>()
            .AsNoTracking()
            .Where(i => i.DecisionStatus == WarehouseInspectionDecisionStatus.Rejected)
            .Where(i => !dbContext.Set<WarehouseToSellerShipment>()
                .Any(s => s.WarehouseInspectionId == i.Id
                       && s.Status != WarehouseToSellerShipmentStatus.Closed
                       && s.Status != WarehouseToSellerShipmentStatus.ReturnedToWarehouse));

        var totalCount = await query.CountAsync(cancellationToken);

        var inspections = await query
            .OrderByDescending(i => i.ReviewedAt ?? i.ModifiedAt ?? i.CreatedAt)
            .Page(parameters)
            .ToListAsync(cancellationToken);

        if (inspections.Count == 0)
            return PagedList<PendingInspectionRejectReturnDto>.Empty();

        var warehouseItemIds = inspections.Select(i => i.WarehouseItemId).Distinct().ToList();
        var warehouseItems = await dbContext.Set<WarehouseItem>()
            .AsNoTracking()
            .Where(w => warehouseItemIds.Contains(w.Id))
            .ToListAsync(cancellationToken);
        var warehouseItemsById = warehouseItems.ToDictionary(w => w.Id);

        var itemIds = warehouseItems.Select(w => ItemId.From(w.ItemId)).Distinct().ToList();
        var items = itemIds.Count == 0
            ? new List<Item>()
            : await dbContext.Set<Item>()
                .AsNoTracking()
                .Include(i => i.Media)
                .Where(i => itemIds.Contains(i.Id))
                .ToListAsync(cancellationToken);
        var itemsById = items.ToDictionary(i => i.Id.Value);

        var sellerIds = items.Select(i => i.SellerId.Value).Distinct().ToList();
        var sellerIdsWithDefaultAddress = sellerIds.Count == 0
            ? new List<Guid>()
            : await dbContext.Set<UserAddress>()
                .AsNoTracking()
                .Where(a => sellerIds.Contains(a.UserId.Value) && a.IsDefault)
                .Select(a => a.UserId.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
        var sellerDefaultAddressSet = sellerIdsWithDefaultAddress.ToHashSet();

        var rows = inspections.Select(inspection =>
        {
            warehouseItemsById.TryGetValue(inspection.WarehouseItemId, out var warehouseItem);

            Item? item = null;
            if (warehouseItem is not null)
                itemsById.TryGetValue(warehouseItem.ItemId, out item);

            var primaryMedia = item?.Media.FirstOrDefault(m => m.IsPrimary)
                ?? item?.Media.FirstOrDefault();
            var sellerId = item?.SellerId.Value;

            return new PendingInspectionRejectReturnDto(
                InspectionId: inspection.Id.Value,
                WarehouseItemId: inspection.WarehouseItemId.Value,
                InboundShipmentId: inspection.InboundShipmentId.Value,
                ItemId: inspection.ItemId,
                SellerId: sellerId,
                ItemTitle: item?.Title.Value,
                PrimaryImageUrl: primaryMedia?.Info.SecureUrl,
                RejectionReason: inspection.DecisionReason,
                ReviewedAt: inspection.ReviewedAt,
                CreatedAt: inspection.CreatedAt,
                DecisionStatus: inspection.DecisionStatus.Id,
                WarehouseItemStatus: warehouseItem?.Status.Id,
                SellerHasDefaultAddress: sellerId.HasValue && sellerDefaultAddressSet.Contains(sellerId.Value));
        }).ToList();

        return rows.ToPagedList(totalCount, parameters);
    }
}
