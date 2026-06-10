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
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyAuctions;

internal sealed class GetMyAuctionsQueryHandler
    : IQueryHandler<GetMyAuctionsQuery, PagedList<AuctionListItemDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IRuntimeSettings _runtimeSettings;
    private readonly IClock _clock;

    public GetMyAuctionsQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser,
        IRuntimeSettings runtimeSettings,
        IClock clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _runtimeSettings = runtimeSettings;
        _clock = clock;
    }

    public async Task<Result<PagedList<AuctionListItemDto>, Error>> Handle(
        GetMyAuctionsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var nowUtc = _clock.UtcNow;

        var query = _dbContext.Set<Auction>()
            .AsNoTracking()
            .Where(a => a.Item.SellerId == _currentUser.UserId);

        // Status filter
        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = AuctionStatus.FromId(parameters.Status);
            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }
        }

        // Search by item title (case-insensitive, whitespace-trimmed)
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var term = parameters.Search.Trim().ToLower();
            query = query.Where(a => a.Item.Title.Value.ToLower().Contains(term));
        }

        query = query.ApplySort(parameters, AuctionMappings.AuctionListItemDtoSortMapping);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var extensionThresholdMinutes = _runtimeSettings.Auction.ExtensionThreshold;

        var pagedAuctions = await query
            .Include(x => x.Item)
                .ThenInclude(x => x.Media)
            .Include(x => x.BuyNowReservations)
            .AsSplitQuery()
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        var myAuctions = pagedAuctions.Items
            .Select(x => x.ToListItemDto(nowUtc, extensionThresholdMinutes))
            .ToList();

        return myAuctions.ToPagedList(totalCount, parameters);
    }
}

