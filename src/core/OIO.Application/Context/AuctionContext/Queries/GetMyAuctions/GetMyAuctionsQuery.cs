using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Abstractions.Sorting;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Queries.Filters;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyAuctions;

public sealed record GetMyAuctionsQuery(
    MyAuctionFilterParameters Parameters) : IQuery<PagedList<AuctionListItemDto>>;
    
internal sealed class GetMyAuctionsQueryHandler
    : IQueryHandler<GetMyAuctionsQuery, PagedList<AuctionListItemDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly SortMappingProvider _sortMappingProvider;
    private readonly IAppConfigs _appConfigs;
    private readonly IClock _clock;

    public GetMyAuctionsQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser,
        IAppConfigs appConfigs,
        SortMappingProvider sortMappingProvider,
        IClock clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _sortMappingProvider = sortMappingProvider;
        _appConfigs = appConfigs;
        _clock = clock;
    }

    public async Task<Result<PagedList<AuctionListItemDto>, Error>> Handle(
        GetMyAuctionsQuery request,
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
            .Where(a => a.SellerId == _currentUser.UserId)
            .Join(
                _dbContext.Set<Item>().AsNoTracking(),
                a => a.ItemId,
                i => i.Id,
                (a, i) => new { Auction = a, Item = i });

        // Status filter
        if (!string.IsNullOrWhiteSpace(parameters.Status) && AuctionStatus.Is(parameters.Status))
        {
            var status = AuctionStatus.FromId(parameters.Status);
            query = query.Where(x => x.Auction.Status == status);
        }

        var sortMapping = _sortMappingProvider.GetMappings<AuctionListItemDto, Auction>();
        // ===== SORTING =====
        //query.ApplySort(parameters, sortMapping);

        var totalCount = await query.CountAsync(cancellationToken);
        var extensionThresholdMinutes = await _appConfigs.Auctions.GetExtensionThresholdMinutesAsync(cancellationToken);
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