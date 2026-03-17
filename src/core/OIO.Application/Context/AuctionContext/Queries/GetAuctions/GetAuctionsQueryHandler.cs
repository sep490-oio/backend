using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;
using CategoryId = OIO.Domain.Context.CatalogContext.ValueObjects.Ids.CategoryId;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctions;

internal sealed class GetAuctionsQueryHandler
    : IQueryHandler<GetAuctionsQuery, PagedList<AuctionListItemDto>>
{
    private readonly IDbContext _dbContext;
    private readonly IClock _clock;
    private readonly IRuntimeSettings _runtimeSettings;

    public GetAuctionsQueryHandler(
        IDbContext dbContext,
        IClock clock,
        IRuntimeSettings runtimeSettings)
    {
        _dbContext = dbContext;
        _clock = clock;
        _runtimeSettings = runtimeSettings;
    }

    public async Task<Result<PagedList<AuctionListItemDto>, Error>> Handle(
        GetAuctionsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var nowUtc = _clock.UtcNow;
        
        var query = _dbContext.Set<Auction>()
            .AsNoTracking();

        // ===== FILTERS =====

        // Status filter
        if (!string.IsNullOrWhiteSpace(parameters.Status) && AuctionStatus.Is(parameters.Status))
        {
            var status = AuctionStatus.FromId(parameters.Status);
            query = query.Where(x => x.Status == status.Value);
        }
        else
        {
            // Default: show only active auctions for public listing
            query = query.Where(x => x.Status == AuctionStatus.Active);
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
                x.Item.Title.Value.ToLower().Contains(searchTerm) ||
                (x.Item.Description != null && x.Item.Description.ToLower().Contains(searchTerm)));
        }

        // Price range
        if (parameters.MinPrice.HasValue)
        {
            query = query.Where(x => x.Pricing.CurrentAmount >= parameters.MinPrice.Value);
        }

        if (parameters.MaxPrice.HasValue)
        {
            query = query.Where(x => x.Pricing.CurrentAmount <= parameters.MaxPrice.Value);
        }

        // Ending within N hours
        if (parameters.EndingWithinHours.HasValue)
        {
            var deadline = nowUtc.AddHours(parameters.EndingWithinHours.Value);
            query = query.Where(x => x.Info.EndTime <= deadline);
        }

        // Featured
        if (parameters.IsFeatured.HasValue && parameters.IsFeatured.Value)
        {
            query = query.Where(x => x.IsFeatured);
        }

        query = query.ApplySort(parameters, AuctionMappings.AuctionListItemDtoSortMapping);

        // ===== COUNT =====
        var totalCount = await query.CountAsync(cancellationToken);
        
        var extensionThresholdMinutes = _runtimeSettings.Auction.ExtensionThreshold;

        var pagedAuctions = await query
            .Include(x => x.Item)
                .ThenInclude(x => x.Media)
            .Include(x => x.BuyNowReservations)
            .AsSplitQuery()
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        var auctions = pagedAuctions.Items
            .Select(x => x.ToListItemDto(nowUtc, extensionThresholdMinutes))
            .ToList();

        return auctions.ToPagedList(pagedAuctions.Metadata);
    }
}

