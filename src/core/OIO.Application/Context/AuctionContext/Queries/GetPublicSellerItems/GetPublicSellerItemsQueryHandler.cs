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
using OIO.Domain.Context.UserContext.Errors;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetPublicSellerItems;

internal sealed class GetPublicSellerItemsQueryHandler(IDbContext dbContext)
    : IQueryHandler<GetPublicSellerItemsQuery, PagedList<PublicSellerItemDto>>
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

    public async Task<Result<PagedList<PublicSellerItemDto>, Error>> Handle(
        GetPublicSellerItemsQuery request,
        CancellationToken cancellationToken)
    {
        var sellerId = UserId.From(request.SellerId);
        var sellerExists = await dbContext.Set<SellerProfile>()
            .AsNoTracking()
            .AnyAsync(x => x.Id == sellerId, cancellationToken);

        if (!sellerExists)
            return UserErrors.SellerProfile.NotFoundById(sellerId);

        var query = dbContext.Set<Item>()
            .AsNoTracking()
            .Where(x =>
                x.SellerId == sellerId &&
                (x.Status == ItemStatus.Approved ||
                 x.Status == ItemStatus.Active ||
                 x.Status == ItemStatus.InAuction ||
                 x.Status == ItemStatus.Sold))
            .OrderByDescending(x => x.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var pagedItems = await query
            .Select(item => new PublicSellerItemDto(
                Id: item.Id.Value,
                SellerId: item.SellerId.Value,
                CategoryId: item.CategoryId.HasValue ? item.CategoryId.Value.Value : null,
                Title: item.Title.Value,
                Description: item.Description,
                Condition: item.Condition.Id,
                Status: item.Status.Id,
                Quantity: item.Quantity,
                Images: item.Media
                    .Select(media =>
                        new ItemMediaDto(
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
                Auction: null,
                HasLiveAuction: false))
            .ToPagedListAsync(totalCount, request.Parameters, cancellationToken);

        var itemIds = pagedItems.Items.Select(x => x.Id).ToList();
        if (itemIds.Count == 0)
            return pagedItems;

        var auctions = await dbContext.Set<Auction>()
            .AsNoTracking()
            .Where(x => itemIds.Contains(x.ItemId.Value))
            .ToListAsync(cancellationToken);

        var auctionLookup = auctions
            .Where(x => PublicAuctionSummaryStatuses.Contains(x.Status.Id, StringComparer.Ordinal))
            .GroupBy(x => x.ItemId.Value)
            .ToDictionary(
                x => x.Key,
                x => x
                    .OrderByDescending(a => a.CreatedAt)
                    .First());

        var items = pagedItems.Items
            .Select(item =>
            {
                if (!auctionLookup.TryGetValue(item.Id, out var auction))
                    return item;

                var summary = new PublicSellerItemAuctionSummaryDto(
                    AuctionId: auction.Id.Value,
                    AuctionStatus: auction.Status.Id,
                    AuctionType: auction.AuctionType?.Id ?? Domain.Context.AuctionContext.Enums.AuctionType.Regular.Id,
                    CurrentPrice: auction.Pricing.CurrentAmount,
                    Currency: auction.Pricing.Currency.Id,
                    StartTime: auction.Info?.StartTime,
                    EndTime: auction.Info?.EndTime);

                return item with
                {
                    Auction = summary,
                    HasLiveAuction = LiveAuctionStatuses.Contains(auction.Status.Id, StringComparer.Ordinal)
                };
            })
            .ToList();

        return new PagedList<PublicSellerItemDto>(
            items,
            pagedItems.Metadata.TotalCount,
            pagedItems.Metadata.CurrentPage,
            pagedItems.Metadata.PageSize);
    }
}
