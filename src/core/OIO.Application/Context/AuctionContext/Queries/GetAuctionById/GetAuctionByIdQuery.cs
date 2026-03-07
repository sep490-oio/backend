using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctionById;

public sealed record GetAuctionByIdQuery(Guid AuctionId) : IQuery<AuctionDetailDto>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetAuctionByIdQuery.Check()
            .WithOwnerName("GetAuctionById")
            .Field(AuctionId)
            .NotEmptyGuid();
    }
}

internal sealed class GetAuctionByIdQueryHandler
    : IQueryHandler<GetAuctionByIdQuery, AuctionDetailDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IAppConfigs _appConfigs;
    
    public GetAuctionByIdQueryHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        IAppConfigs appConfigs)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _appConfigs = appConfigs;
    }

    public async Task<Result<AuctionDetailDto, Error>> Handle(
        GetAuctionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .Include(a => Enumerable.OrderByDescending<Bid, DateTime>(a.Bids, b => b.CreatedAt))
                .Include(a => a.AutoBids)
                .Include(a => a.Watchers)
                .Include(a => a.PriceHistories.OrderByDescending(ph => ph.RecordedAt))
                .AsSplitQuery(),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        var nowUtc = _clock.UtcNow;
        // Increment view count
        auction.IncrementView(nowUtc);

        var item = await _dbContext.GetByIdAsync<Item, ItemId>(
            id: auction.ItemId,
            queryBuilder: query => query
                .Include(i => i.Media.OrderBy(img => img.SortOrder)),
            cancellationToken: cancellationToken);

        if (item is null)
            return AuctionErrors.Item.NotFound(auction.ItemId);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuctionDetailDto(
            Auction: auction.ToDto(nowUtc, await _appConfigs.Auctions.GetExtensionThresholdMinutesAsync(cancellationToken)),
            Item: item.ToDto(),
            RecentBids: auction.Bids
                .OrderByDescending(b => b.CreatedAt)
                .Take(20)
                .Select(b => b.ToDto())
                .ToList(),
            PriceHistory: auction.PriceHistories
                .OrderByDescending(ph => ph.RecordedAt)
                .Select(ph => ph.ToDto())
                .ToList());
    }
}