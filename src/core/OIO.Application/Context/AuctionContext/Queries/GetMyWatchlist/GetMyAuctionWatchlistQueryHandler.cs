using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyWatchlist;

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
            .Where(w => w.UserId == _currentUser.UserId);

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
                AuctionId: x.Auction.Id.Value,
                ItemTitle: x.Auction.Item.Title,
                PrimaryImageUrl: x.Auction.Item.Media
                    .Where(img => img.IsPrimary)
                    .Select(img => img.Url)
                    .FirstOrDefault(),
                CurrentPrice: x.Auction.CurrentPrice.Amount,
                Currency: x.Auction.StartingPrice.Currency.Id,
                AuctionStatus: x.Auction.Status.Id,
                BidCount: x.Auction.BidCount,
                EndTime: x.Auction.Duration.EndTime,
                RemainingTime: x.Auction.Duration.EndTime - nowUtc,
                NotifyOnBid: x.NotifyOnBid,
                NotifyOnEnd: x.NotifyOnEnd,
                WatchedAt: x.CreatedAt))
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return items;
    }
}