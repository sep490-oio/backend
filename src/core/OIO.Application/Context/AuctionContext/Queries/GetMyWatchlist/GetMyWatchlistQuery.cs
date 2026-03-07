using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.Queries.Filters;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Items;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyWatchlist;

public sealed record GetMyAuctionWatchlistQuery(
    MyWatchlistFilterParameters Parameters) : IQuery<PagedList<MyAuctionWatchlistDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetMyAuctionWatchlistQuery.Check()
            .WithOwnerName("GetMyAuctionWatchlist")
            .Field(Parameters.AuctionStatus)
            .WhenHasValue(x => x.InSet(AuctionStatus.All.Select(y => y.Id)));
    }
}

internal sealed class GetMyAuctionWatchlistQueryHandler
    : IQueryHandler<GetMyAuctionWatchlistQuery, PagedList<MyAuctionWatchlistDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public GetMyAuctionWatchlistQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser,
        IClock clock)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<PagedList<MyAuctionWatchlistDto>, Error>> Handle(
        GetMyAuctionWatchlistQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var nowUtc = _clock.UtcNow;

        var query = _dbContext.Set<AuctionWatcher>()
            .AsNoTracking()
            .Where(w => w.UserId == _currentUser.UserId)
            .Join(
                _dbContext.Set<Auction>().AsNoTracking(),
                w => w.AuctionId,
                a => a.Id,
                (w, a) => new { Watcher = w, Auction = a })
            .Join(
                _dbContext.Set<Item>().AsNoTracking(),
                x => x.Auction.ItemId,
                i => i.Id,
                (x, i) => new { x.Watcher, x.Auction, Item = i });

        // Auction status filter
        if (!string.IsNullOrWhiteSpace(parameters.AuctionStatus))
        {
            var status = AuctionStatus.FromId(parameters.AuctionStatus);
            query = query.Where(x => x.Auction.Status == status);
        }

        // TODO: implement sorting for Get My Watch list Query 
        

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Page(parameters)
            .Select(x => new MyAuctionWatchlistDto(
                x.Auction.Id.Value,
                x.Item.Title,
                x.Item.Media
                    .Where(img => img.IsPrimary)
                    .Select(img => img.Url)
                    .FirstOrDefault(),
                x.Auction.CurrentPrice.Amount,
                x.Auction.Currency,
                x.Auction.Status.Id,
                x.Auction.BidCount,
                x.Auction.Duration.EndTime,
                x.Auction.Duration.EndTime - nowUtc,
                x.Watcher.NotifyOnBid,
                x.Watcher.NotifyOnEnd,
                x.Watcher.CreatedAt))
            .ToListAsync(cancellationToken);

        return PagedList<MyAuctionWatchlistDto>.ToPagedList(
            items, totalCount, parameters);
    }
}