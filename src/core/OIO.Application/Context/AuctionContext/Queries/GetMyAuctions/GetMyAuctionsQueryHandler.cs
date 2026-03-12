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
    private readonly IAppConfigs _appConfigs;
    private readonly IClock _clock;

    public GetMyAuctionsQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser,
        IAppConfigs appConfigs,
        IClock clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _appConfigs = appConfigs;
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
        if (!string.IsNullOrWhiteSpace(parameters.Status) && AuctionStatus.Is(parameters.Status))
        {
            var status = AuctionStatus.FromId(parameters.Status);
            query = query.Where(x => x.Status == status);
        }

        query = query.ApplySort(parameters, AuctionMappings.AuctionListItemDtoSortMapping);

        var totalCount = await query.CountAsync(cancellationToken);
        
        var extensionThresholdMinutes = await _appConfigs.Auctions.GetExtensionThresholdMinutesAsync(cancellationToken);
        
        var myAuctions = await query
            .Select(x => new AuctionListItemDto(
                Id: x.Id.Value,
                ItemTitle: x.Item.Title.Value,
                PrimaryImageUrl: x.Item.Media
                    .Where(img => img.IsPrimary)
                    .Select(img => img.Info.SecureUrl)
                    .FirstOrDefault(),
                CurrentPrice: x.Pricing.CurrentPrice.ToDto(),
                StartingPrice: x.Pricing.StartingPrice.ToDto(),
                BuyNowPrice: x.Pricing.BuyNowPrice != null ? x.Pricing.BuyNowPrice.ToDto() : null,
                Currency:  x.Pricing.Currency.Id,
                Status: x.Status.Id,
                BidCount: x.BidCount,
                WatchCount: x.WatchCount,
                StartTime: x.Info.StartTime,
                EndTime: x.Info.EndTime,
                RemainingTime: x.Info.RemainingTime(nowUtc),
                IsEndingSoon: x.IsEndingSoon(nowUtc, extensionThresholdMinutes),
                IsFeatured: x.IsFeatured,
                SellerId: x.Item.SellerId.Value))
            .ToPagedListAsync(totalCount,parameters, cancellationToken);

        return myAuctions;
    }
}