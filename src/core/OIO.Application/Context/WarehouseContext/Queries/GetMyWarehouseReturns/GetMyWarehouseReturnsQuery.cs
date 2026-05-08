using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Context.WarehouseContext.DTOs;
using OIO.Application.Context.WarehouseContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseToSellerShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;
using ItemId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId;

namespace OIO.Application.Context.WarehouseContext.Queries.GetMyWarehouseReturns;

/// <summary>
/// Seller-scoped feed of <see cref="WarehouseToSellerShipment"/> rows. The
/// <see cref="Status"/> filter is permissive: values outside the enum are
/// logged and fall back to no status filter (matches the permissive pattern
/// used by <c>GetMyWithdrawalsQuery</c>).
/// </summary>
public sealed record GetMyWarehouseReturnsQuery(
    PagedParameters Parameters,
    string? Status = null)
    : IQuery<PagedList<WarehouseToSellerShipmentDto>>;

internal sealed class GetMyWarehouseReturnsQueryHandler(
    IDbContext dbContext,
    ICurrentUser currentUser,
    ILogger<GetMyWarehouseReturnsQueryHandler> logger)
    : IQueryHandler<GetMyWarehouseReturnsQuery, PagedList<WarehouseToSellerShipmentDto>>
{
    public async Task<Result<PagedList<WarehouseToSellerShipmentDto>, Error>> Handle(
        GetMyWarehouseReturnsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var sellerId = currentUser.UserId;

        var query = dbContext.Set<WarehouseToSellerShipment>()
            .AsNoTracking()
            .Include(s => s.Evidence)
            .Where(s => s.SellerId == sellerId);

        if (!string.IsNullOrWhiteSpace(request.Status)
            && !string.Equals(request.Status, "all", StringComparison.OrdinalIgnoreCase))
        {
            var statusVo = WarehouseToSellerShipmentStatus.FromId(request.Status.Trim().ToLowerInvariant());
            if (statusVo.HasValue)
            {
                var concrete = statusVo.Value;
                query = query.Where(s => s.Status == concrete);
            }
            else
            {
                logger.LogWarning(
                    "GetMyWarehouseReturns: unknown status filter '{Status}' for seller {SellerId}. Returning all.",
                    request.Status, sellerId.Value);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var shipments = await query
            .OrderByDescending(s => s.CreatedAt)
            .Page(parameters)
            .ToListAsync(cancellationToken);

        if (shipments.Count == 0)
            return PagedList<WarehouseToSellerShipmentDto>.Empty();

        // Batch-enrich warehouse items + item titles.
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
