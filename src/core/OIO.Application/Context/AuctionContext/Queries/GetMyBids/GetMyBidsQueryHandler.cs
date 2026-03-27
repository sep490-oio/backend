using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
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

namespace OIO.Application.Context.AuctionContext.Queries.GetMyBids;

internal sealed class GetMyBidsQueryHandler
    : IQueryHandler<GetMyBidsQuery, PagedList<MyBidDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyBidsQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedList<MyBidDto>, Error>> Handle(
        GetMyBidsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        // Get the latest bid per auction for this bidder
        // Subquery: get the max bid ID per auction (latest = highest ID for same auction)
        var latestBidIds = _dbContext.Set<Bid>()
            .AsNoTracking()
            .Where(bid => bid.BidderId == _currentUser.UserId)
            .GroupBy(bid => bid.AuctionId)
            .Select(g => g.OrderByDescending(b => b.CreatedAt).First().Id);

        var query = _dbContext.Set<Bid>()
            .AsNoTracking()
            .Where(bid => latestBidIds.Contains(bid.Id));

        // Status filter
        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var bidStatus = BidStatus.FromId(parameters.Status);
            query = query.Where(b => b.Status == bidStatus);
        }

        query = query.ApplySort(parameters, BidMappings.MyBidDtoSortMapping);

        var totalCount = await query.CountAsync(cancellationToken);

        var myBids = await query
            .Select(x => new MyBidDto(
                x.Id.Value,
                x.Auction.Id.Value,
                x.Auction.Item.Title.Value,
                x.Auction.Item.Media
                    .Where(img => img.IsPrimary)
                    .Select(img => img.Info.SecureUrl)
                    .FirstOrDefault(),
                x.Amount.ToDto(),
                x.Auction.Pricing.CurrentPrice.ToDto(),
                x.Status.Id,
                x.Auction.Status.Id,
                x.Status == BidStatus.Winning,
                x.CreatedAt,
                x.Auction.Info.EndTime))
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return myBids;
    }
}