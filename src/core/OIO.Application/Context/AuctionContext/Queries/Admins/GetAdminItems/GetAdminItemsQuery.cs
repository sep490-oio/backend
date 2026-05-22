using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Extensions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Aggregates.WarehouseItems;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;
using OIO.Domain.Context.WarehouseContext.Enums;

namespace OIO.Application.Context.AuctionContext.Queries.Admins.GetAdminItems;

public sealed record GetAdminItemsQuery(
    AdminItemFilterParameters Parameters) : IQuery<PagedList<AdminItemListItemDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetAdminItemsQuery.Check()
            .WithOwnerName("GetAdminItems")
            .Field(Parameters.Status)
            .WhenHasValue(x => x.InSet(ItemStatus.All.Select(status => status.Id)))
            .Field(Parameters.CategoryId)
            .WhenHasValue(x => x.NotEmptyGuid());
    }
}

internal sealed class GetAdminItemsQueryHandler
    : IQueryHandler<GetAdminItemsQuery, PagedList<AdminItemListItemDto>>
{
    private readonly IDbContext _dbContext;

    public GetAdminItemsQueryHandler(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PagedList<AdminItemListItemDto>, Error>> Handle(
        GetAdminItemsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
            
        var query = _dbContext.Set<Item>()
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = ItemStatus.FromId(parameters.Status).Value;
            query = query.Where(i => i.Status == status);
        }

        if (parameters.CategoryId.HasValue)
        {
            query = query.Where(i => i.CategoryId != null && i.CategoryId.Value == parameters.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Condition))
        {
            var condition = ItemCondition.FromId(parameters.Condition);
            if (condition.HasValue)
                query = query.Where(i => i.Condition == condition.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var searchTerm = parameters.SearchTerm.Trim().ToLower();
            query = query.Where(i => i.Title.Value.ToLower().Contains(searchTerm) || i.Id.Value.ToString().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(parameters.PhysicalLocation))
        {
            var loc = parameters.PhysicalLocation.ToLower();
            var warehouseItemsQuery = _dbContext.Set<WarehouseItem>().Select(wi => wi.ItemId);
            
            if (loc == "warehouse")
            {
                query = query.Where(i => _dbContext.Set<WarehouseItem>().Any(wi => wi.ItemId == i.Id));
            }
            else if (loc == "seller")
            {
                query = query.Where(i => !_dbContext.Set<WarehouseItem>().Any(wi => wi.ItemId == i.Id));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedItems = await query
            .OrderByDescending(i => i.CreatedAt)
            .Include(item => item.Auctions)
            .Include(item => item.Media)
            .AsSplitQuery()
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        var sellerIds = pagedItems.Items.Select(x => x.SellerId).Distinct().ToList();
        var sellerNameLookup = await _dbContext.Set<SellerProfile>()
            .AsNoTracking()
            .Where(x => sellerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.StoreName, cancellationToken);

        var itemIds = pagedItems.Items.Select(x => x.Id.Value).ToList();

        // Query physical location: Check WarehouseItems
        var warehouseItems = itemIds.Count > 0 ? await _dbContext.Set<WarehouseItem>()
            .AsNoTracking()
            .Where(wi => itemIds.Contains(wi.ItemId))
            .ToListAsync(cancellationToken) : [];

        var wiByItemId = warehouseItems.ToDictionary(wi => wi.ItemId);

        var items = pagedItems.Items
            .Select(item =>
            {
                sellerNameLookup.TryGetValue(item.SellerId, out var sellerName);
                var primaryImage = item.Media
                    .OrderBy(m => m.IsPrimary ? 0 : 1)
                    .ThenBy(m => m.SortOrder)
                    .FirstOrDefault();

                var currentAuction = item.Auctions.OrderByDescending(a => a.CreatedAt).FirstOrDefault();

                string? physicalLocation = null;
                if (wiByItemId.TryGetValue(item.Id.Value, out var wi))
                {
                    if (wi.Status == WarehouseItemStatus.Stored && wi.StorageLocationId != null)
                        physicalLocation = $"Warehouse: {wi.StorageLocationId}";
                    else
                        physicalLocation = $"Warehouse ({wi.Status.Id})";
                }
                else
                {
                    physicalLocation = "With Seller";
                }

                // If filter PhysicalLocation is provided, we do it in-memory here since it's hard to join
                // across domains efficiently. For exact requirements, cross-domain querying should be done via read-models.
                
                return new AdminItemListItemDto(
                    Id: item.Id.Value,
                    SellerId: item.SellerId.Value,
                    SellerDisplayName: sellerName ?? "Unknown",
                    CategoryId: item.CategoryId?.Value,
                    Title: item.Title.Value,
                    Condition: item.Condition.Id,
                    Status: item.Status.Id,
                    PrimaryImageUrl: primaryImage?.Info.SecureUrl ?? "",
                    CreatedAt: item.CreatedAt,
                    CurrentPhysicalLocation: physicalLocation,
                    CurrentAuctionId: currentAuction?.Id.Value,
                    CurrentAuctionStatus: currentAuction?.Status.Id
                );
            })
            .ToList();

        return items.ToPagedList(pagedItems.Metadata);
    }
}
