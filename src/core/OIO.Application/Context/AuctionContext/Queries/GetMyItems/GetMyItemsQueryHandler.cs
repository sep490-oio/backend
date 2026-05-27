using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.WarehouseContext.Aggregates.InboundShipments;
using OIO.Domain.Context.WarehouseContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyItems;

internal sealed class GetMyItemsQueryHandler
    : IQueryHandler<GetMyItemsQuery, PagedList<ItemDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyItemsQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedList<ItemDto>, Error>> Handle(
        GetMyItemsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var query = _dbContext.Set<Item>()
            .AsNoTracking()
            .Where(i => i.SellerId == _currentUser.UserId);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var term = parameters.Search.ToLower();
            query = query.Where(i => i.Title.Value.ToLower().Contains(term));
        }

        // Server-side status filter — was previously missing, which made the
        // /seller/items status pills effectively no-ops.
        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var statusId = parameters.Status;
            query = query.Where(i => i.Status.Id == statusId);
        }

        // requiresPlatformInspection is a canonical persisted flag on Item,
        // set at Submit/Resubmit time.
        if (parameters.RequiresPlatformInspection.HasValue)
        {
            var flag = parameters.RequiresPlatformInspection.Value;
            query = query.Where(i => i.RequiresPlatformInspection == flag);
        }

        // hasActiveInbound = item has an InboundShipment whose status is neither
        // Cancelled nor Failed. The inbound-book picker passes hasActiveInbound=false
        // so a re-attempt is allowed once a previous shipment is cancelled/failed.
        var activeInboundItemGuids = await _dbContext.Set<InboundShipment>()
            .Where(s => s.SellerId == _currentUser.UserId &&
                        s.Status != InboundShipmentStatus.Cancelled &&
                        s.Status != InboundShipmentStatus.Failed)
            .Select(s => s.ItemId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (parameters.HasActiveInbound.HasValue)
        {
            var wantsActive = parameters.HasActiveInbound.Value;
            var activeInboundItemIds = activeInboundItemGuids
                .Select(g => OIO.Domain.Context.CatalogContext.ValueObjects.Ids.ItemId.From(g))
                .ToList();

            query = wantsActive
                ? query.Where(i => activeInboundItemIds.Contains(i.Id))
                : query.Where(i => !activeInboundItemIds.Contains(i.Id));
        }

        query = query.ApplySort(parameters, ItemMappings.ItemDtoSortMapping);

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedItems = await query
            .Include(item => item.Media)
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        var pagedItemIds = pagedItems.Items.Select(x => x.Id).ToList();
        
        var auctions = await _dbContext.Set<OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Auction>()
            .AsNoTracking()
            .Where(a => pagedItemIds.Contains(a.ItemId))
            .ToListAsync(cancellationToken);

        var auctionsByItemId = auctions
            .GroupBy(a => a.ItemId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.CreatedAt).ToList());

        var itemDtos = pagedItems.Items
            .Select(item =>
            {
                bool hasLiveAuction = false;
                ItemAuctionSummaryDto? auctionSummary = null;

                if (auctionsByItemId.TryGetValue(item.Id, out var itemAuctions) && itemAuctions.Count > 0)
                {
                    hasLiveAuction = itemAuctions.Any(a => a.Status == OIO.Domain.Context.AuctionContext.Enums.AuctionStatus.Active);
                    var latest = itemAuctions.First();
                    auctionSummary = new ItemAuctionSummaryDto(
                        AuctionId: latest.Id.Value,
                        AuctionStatus: latest.Status.Id,
                        AuctionType: latest.AuctionType?.Id ?? "Regular",
                        CurrentPrice: latest.Pricing.CurrentAmount,
                        Currency: latest.Pricing.Currency.Id,
                        StartTime: latest.Info?.StartTime,
                        EndTime: latest.Info?.EndTime);
                }

                return item.ToDto(
                    hasInboundShipment: activeInboundItemGuids.Contains(item.Id.Value),
                    hasLiveAuction: hasLiveAuction,
                    auction: auctionSummary);
            })
            .ToList();

        return new PagedList<ItemDto>(itemDtos, pagedItems.Metadata);
    }
}