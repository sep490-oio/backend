using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Queries.GetPendingStaffReturns;

/// <summary>
/// Warehouse-staff feed of <see cref="WarehouseToSellerShipment"/> rows. Default
/// filter is <c>pending</c> (the staff "ready to ship" queue). <c>status=all</c>
/// returns every row. Warehouse-scoping is not applied in V1 — per plan D6 there
/// is no warehouse-assignment model yet; V2 can narrow by staff warehouse.
/// </summary>
public sealed record GetPendingStaffReturnsQuery(
    PagedParameters Parameters,
    string? Status = "pending")
    : IQuery<PagedList<WarehouseToSellerShipmentDto>>;

internal sealed class GetPendingStaffReturnsQueryHandler(
    IDbContext dbContext,
    ILogger<GetPendingStaffReturnsQueryHandler> logger)
    : IQueryHandler<GetPendingStaffReturnsQuery, PagedList<WarehouseToSellerShipmentDto>>
{
    public async Task<Result<PagedList<WarehouseToSellerShipmentDto>, Error>> Handle(
        GetPendingStaffReturnsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        var query = dbContext.Set<WarehouseToSellerShipment>()
            .AsNoTracking();

        var rawStatus = string.IsNullOrWhiteSpace(request.Status)
            ? "pending"
            : request.Status.Trim().ToLowerInvariant();

        if (!string.Equals(rawStatus, "all", StringComparison.OrdinalIgnoreCase))
        {
            var statusVo = WarehouseToSellerShipmentStatus.FromId(rawStatus);
            if (statusVo.HasValue)
            {
                var concrete = statusVo.Value;
                query = query.Where(s => s.Status == concrete);
            }
            else
            {
                logger.LogWarning(
                    "GetPendingStaffReturns: unknown status filter '{Status}'. Returning all rows.",
                    request.Status);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var shipments = await query
            .OrderBy(s => s.CreatedAt)
            .Page(parameters)
            .ToListAsync(cancellationToken);

        if (shipments.Count == 0)
            return PagedList<WarehouseToSellerShipmentDto>.Empty();

        var warehouseItemIds = shipments.Select(s => s.WarehouseItemId).Distinct().ToList();
        var warehouseItems = await dbContext.Set<WarehouseItem>()
            .AsNoTracking()
            .Include(w => w.Media)
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
        var itemsByIdValue = items.ToDictionary(i => i.Id.Value);

        var dtos = shipments.Select(s =>
        {
            WarehouseToSellerShipmentItemSummaryDto? summary = null;
            if (warehouseItemsById.TryGetValue(s.WarehouseItemId, out var wi))
            {
                itemsByIdValue.TryGetValue(wi.ItemId, out var item);
                var primary = item?.Media.FirstOrDefault(m => m.IsPrimary) ?? item?.Media.FirstOrDefault();
                summary = wi.ToSummary(item?.Title.Value, primary?.Info.SecureUrl);
            }
            return s.ToDto(summary);
        }).ToList();

        return dtos.ToPagedList(totalCount, parameters);
    }
}
