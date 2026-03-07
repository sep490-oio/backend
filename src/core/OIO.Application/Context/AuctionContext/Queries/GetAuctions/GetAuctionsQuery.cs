using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Sorting;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Queries.Filters;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctions;

public sealed record GetAuctionsQuery(
    AuctionFilterParameters Parameters) : IQuery<PagedList<AuctionListItemDto>>;
    
    internal sealed class GetAuctionsQueryHandler
    : IQueryHandler<GetAuctionsQuery, PagedList<AuctionListItemDto>>
{
    private readonly IDbContext _dbContext;
    private readonly IClock _clock;
    private readonly SortMappingProvider _sortMappingProvider;
    private readonly IAppConfigs _appConfigs;

    public GetAuctionsQueryHandler(
        IDbContext dbContext,
        IClock clock,
        SortMappingProvider sortMappingProvider,
        IAppConfigs appConfigs)
    {
        _dbContext = dbContext;
        _clock = clock;
        _sortMappingProvider = sortMappingProvider;
        _appConfigs = appConfigs;
    }

    public async Task<Result<PagedList<AuctionListItemDto>, Error>> Handle(
        GetAuctionsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var nowUtc = _clock.UtcNow;
        
        if (!_sortMappingProvider.ValidateMappings<AuctionListItemDto, Auction>(parameters.SortBy))
        {
            return Error.Validation("SortBy", "SortBy.Invalid",
                $"The provided sort parameter isn't valid: '{parameters.SortBy}'");
        }
        

        var query = _dbContext.Set<Auction>()
            .AsNoTracking()
            .Join(
                _dbContext.Set<Item>().AsNoTracking(),
                a => a.ItemId,
                i => i.Id,
                (a, i) => new { Auction = a, Item = i });

        // ===== FILTERS =====

        // Status filter
        if (!string.IsNullOrWhiteSpace(parameters.Status) && AuctionStatus.Is(parameters.Status))
        {
            var status = AuctionStatus.FromId(parameters.Status);
            query = query.Where(x => x.Auction.Status == status);
        }
        else
        {
            // Default: show only active auctions for public listing
            query = query.Where(x => x.Auction.Status == AuctionStatus.Active);
        }

        // Category filter
        if (parameters.CategoryId.HasValue)
        {
            var categoryId = CategoryId.From(parameters.CategoryId.Value);
            query = query.Where(x => x.Item.CategoryId == categoryId);
        }

        // Search
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var searchTerm = parameters.Search.ToLower();
            query = query.Where(x =>
                x.Item.Title.ToLower().Contains(searchTerm) ||
                (x.Item.Description != null && x.Item.Description.ToLower().Contains(searchTerm)));
        }

        // Price range
        if (parameters.MinPrice.HasValue)
        {
            query = query.Where(x => x.Auction.CurrentPrice.Amount >= parameters.MinPrice.Value);
        }

        if (parameters.MaxPrice.HasValue)
        {
            query = query.Where(x => x.Auction.CurrentPrice.Amount <= parameters.MaxPrice.Value);
        }

        // Ending within N hours
        if (parameters.EndingWithinHours.HasValue)
        {
            var deadline = nowUtc.AddHours(parameters.EndingWithinHours.Value);
            query = query.Where(x => x.Auction.Duration.EndTime <= deadline);
        }

        // Featured
        if (parameters.IsFeatured.HasValue && parameters.IsFeatured.Value)
        {
            query = query.Where(x => x.Auction.IsFeatured);
        }

        var sortMapping = _sortMappingProvider.GetMappings<AuctionListItemDto, Auction>();
        // ===== SORTING =====
        //query.ApplySort(parameters, sortMapping, "Auction.Id");

        // ===== COUNT =====
        var totalCount = await query.CountAsync(cancellationToken);
        var extensionThresholdMinutes = await _appConfigs.Auctions.GetExtensionThresholdMinutesAsync(cancellationToken);
        // ===== PROJECT + PAGE =====
        var items = await query
            .Page(parameters)
            .Select(x => new AuctionListItemDto(
                x.Auction.Id.Value,
                x.Item.Title,
                x.Item.Media
                    .Where(img => img.IsPrimary)
                    .Select(img => img.Url)
                    .FirstOrDefault(),
                x.Auction.CurrentPrice.Amount,
                x.Auction.StartingPrice.Amount,
                x.Auction.BuyNowPrice != null ? x.Auction.BuyNowPrice.Amount : null,
                x.Auction.Currency,
                x.Auction.Status.Id,
                x.Auction.BidCount,
                x.Auction.WatchCount,
                x.Auction.Duration.StartTime,
                x.Auction.Duration.EndTime,
                x.Auction.RemainingTime(nowUtc),
                x.Auction.IsEndingSoon(nowUtc, extensionThresholdMinutes),
                x.Auction.IsFeatured,
                x.Auction.SellerId.Value))
            .ToListAsync(cancellationToken);

        return PagedList<AuctionListItemDto>.ToPagedList(
            items, 
            totalCount,
            parameters);
    }
}