using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.CatalogContext.Aggregates.Items;
using OIO.Domain.Context.CatalogContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.SeedWork.Errors;
using CategoryId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.CategoryId;

namespace OIO.Application.Context.AuctionContext.Queries.GetPublicItems;

public record GetPublicItemsFilterParameters : PagedParameters, ISortByParameter
{
    public Guid? CategoryId { get; init; }
    public string? Search { get; init; }
    public string? Condition { get; init; }
    public string? SortBy { get; init; }
}

public sealed record GetPublicItemsQuery(GetPublicItemsFilterParameters Parameters)
    : IQuery<PagedList<PublicItemDto>>;

internal sealed class GetPublicItemsQueryHandler(IDbContext dbContext)
    : IQueryHandler<GetPublicItemsQuery, PagedList<PublicItemDto>>
{
    private static readonly string[] PublicAuctionSummaryStatuses =
    [
        AuctionStatus.Approved.Id,
        AuctionStatus.Scheduled.Id,
        AuctionStatus.Active.Id,
        AuctionStatus.Ended.Id,
        AuctionStatus.Sold.Id,
        AuctionStatus.PaymentDefaulted.Id
    ];

    private static readonly string[] LiveAuctionStatuses =
    [
        AuctionStatus.Approved.Id,
        AuctionStatus.Scheduled.Id,
        AuctionStatus.Active.Id,
        AuctionStatus.Ended.Id,
        AuctionStatus.PaymentDefaulted.Id
    ];

    public async Task<Result<PagedList<PublicItemDto>, Error>> Handle(
        GetPublicItemsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        var query = dbContext.Set<Item>()
            .AsNoTracking()
            .Where(x =>
                x.Status == ItemStatus.Approved ||
                x.Status == ItemStatus.Active ||
                x.Status == ItemStatus.InAuction);

        // ===== FILTERS =====

        // Category filter
        if (parameters.CategoryId.HasValue)
        {
            var categoryId = CategoryId.From(parameters.CategoryId.Value);
            query = query.Where(x => x.CategoryId == categoryId);
        }

        // Search
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var searchTerm = parameters.Search.ToLower();
            query = query.Where(x =>
                x.Title.Value.ToLower().Contains(searchTerm) ||
                (x.Description != null && x.Description.ToLower().Contains(searchTerm)));
        }

        // Condition filter
        if (!string.IsNullOrWhiteSpace(parameters.Condition))
        {
            var condition = new ItemCondition(parameters.Condition);
            query = query.Where(x => x.Condition == condition);
        }

        query = query.OrderByDescending(x => x.CreatedAt);

        // ===== COUNT =====
        var totalCount = await query.CountAsync(cancellationToken);

        var pagedItems = await query
            .Include(item => item.Media)
            .AsSplitQuery()
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        var itemIds = pagedItems.Items.Select(x => x.Id).ToList();
        if (itemIds.Count == 0)
        {
            return new PagedList<PublicItemDto>(
                [],
                pagedItems.Metadata.TotalCount,
                pagedItems.Metadata.CurrentPage,
                pagedItems.Metadata.PageSize);
        }

        // Fetch auctions for these items
        var auctions = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId))
            .ToListAsync(cancellationToken);

        var auctionLookup = auctions
            .Where(x => PublicAuctionSummaryStatuses.Contains(x.Status.Id, StringComparer.Ordinal))
            .GroupBy(x => x.ItemId)
            .ToDictionary(
                x => x.Key,
                x => x
                    .OrderByDescending(a => a.CreatedAt)
                    .First());

        // Fetch seller names
        var sellerIds = pagedItems.Items.Select(x => x.SellerId).Distinct().ToList();

        var sellerNameLookup = await dbContext.Set<SellerProfile>()
            .AsNoTracking()
            .Where(x => sellerIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.StoreName, cancellationToken);

        var items = pagedItems.Items
            .Select(item =>
            {
                sellerNameLookup.TryGetValue(item.SellerId, out var sellerName);

                if (!auctionLookup.TryGetValue(item.Id, out var auction))
                    return MapItem(item, sellerName, null);

                var summary = new PublicSellerItemAuctionSummaryDto(
                    AuctionId: auction.Id.Value,
                    AuctionStatus: auction.Status.Id,
                    AuctionType: auction.AuctionType?.Id ?? Domain.Context.AuctionContext.Enums.AuctionType.Regular.Id,
                    CurrentPrice: auction.Pricing.CurrentAmount,
                    Currency: auction.Pricing.Currency.Id,
                    StartTime: auction.Info?.StartTime,
                    EndTime: auction.Info?.EndTime);

                return MapItem(
                    item,
                    sellerName,
                    summary,
                    LiveAuctionStatuses.Contains(auction.Status.Id, StringComparer.Ordinal));
            })
            .ToList();

        return new PagedList<PublicItemDto>(
            items,
            pagedItems.Metadata.TotalCount,
            pagedItems.Metadata.CurrentPage,
            pagedItems.Metadata.PageSize);
    }

    private static PublicItemDto MapItem(
        Item item,
        string? sellerName,
        PublicSellerItemAuctionSummaryDto? auction,
        bool hasLiveAuction = false)
    {
        return new PublicItemDto(
            Id: item.Id.Value,
            SellerId: item.SellerId.Value,
            SellerName: sellerName ?? string.Empty,
            CategoryId: item.CategoryId?.Value,
            Title: item.Title.Value,
            Description: item.Description,
            Condition: item.Condition.Id,
            Status: item.Status.Id,
            Quantity: item.Quantity,
            Images: item.Media
                .OrderBy(media => media.SortOrder)
                .Select(media => new ItemMediaDto(
                    Id: media.Id.Value,
                    Url: media.Info.SecureUrl!,
                    PublicId: media.StorageRef.PublicId,
                    ResourceType: media.ResourceType,
                    IsPrimary: media.IsPrimary,
                    SortOrder: media.SortOrder,
                    FileName: media.Info.FileName,
                    Bytes: media.Info.Bytes,
                    Format: media.Info.Format,
                    Width: media.Info.Width,
                    Height: media.Info.Height,
                    DurationSeconds: media.Info.DurationSeconds))
                .ToList(),
            CreatedAt: item.CreatedAt,
            Auction: auction,
            HasLiveAuction: hasLiveAuction);
    }
}
