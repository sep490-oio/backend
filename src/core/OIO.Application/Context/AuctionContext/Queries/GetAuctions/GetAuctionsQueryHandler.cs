using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
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
    private readonly ICurrentUser _currentUser;

    public GetAuctionsQueryHandler(
        IDbContext dbContext,
        IClock clock,
        IRuntimeSettings runtimeSettings,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _clock = clock;
        _runtimeSettings = runtimeSettings;
        _currentUser = currentUser;
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

        // Status filter — StatusGroup-first precedence. Logic lives in
        // GetAuctionsStatusFilter so the precedence rules stay unit-testable without EF.
        query = query.Where(GetAuctionsStatusFilter.Build(parameters.StatusGroup, parameters.Status));

        // Category filter
        if (parameters.CategoryId.HasValue)
        {
            var categoryId = CategoryId.From(parameters.CategoryId.Value);
            query = query.Where(x => x.Item.CategoryId == categoryId);
        }

        // Auction Type filter
        if (!string.IsNullOrWhiteSpace(parameters.AuctionType) && AuctionType.Is(parameters.AuctionType))
        {
            var auctionType = AuctionType.FromId(parameters.AuctionType).Value;
            query = query.Where(x => x.AuctionType == auctionType);
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

        var currentUserId = _currentUser.IsAuthenticated ? (OIO.Domain.Context.UserContext.ValueObjects.Ids.UserId?)_currentUser.UserId : null;
        var watchedAuctionIds = new HashSet<AuctionId>();
        if (currentUserId is not null)
        {
            var auctionIds = pagedAuctions.Items.Select(a => a.Id).ToList();
            var watchedIds = await _dbContext.Set<AuctionWatcher>()
                .Where(w => w.UserId == currentUserId.Value && auctionIds.Contains(w.AuctionId))
                .Select(w => w.AuctionId)
                .ToListAsync(cancellationToken);
            watchedAuctionIds = watchedIds.ToHashSet();
        }

        var auctions = pagedAuctions.Items
            .Select(x => x.ToListItemDto(nowUtc, extensionThresholdMinutes, watchedAuctionIds.Contains(x.Id)))
            .ToList();

        return auctions.ToPagedList(pagedAuctions.Metadata);
    }
}

