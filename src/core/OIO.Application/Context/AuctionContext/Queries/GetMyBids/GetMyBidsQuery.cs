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
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyBids;

public sealed record GetMyBidsQuery(
    MyBidFilterParameters Parameters) : IQuery<PagedList<MyBidDto>>, IHasValidate
{
    public ViolationsError Validate()
    {
        return GetMyBidsQuery.Check()
            .WithOwnerName("GetMyBids")
            .Field(Parameters.Status)
            .WhenHasValue(x => x.InSet(BidStatus.All.Select(s => s.Id)));
    }
}

internal sealed class GetMyBidsQueryHandler
    : IQueryHandler<GetMyBidsQuery, PagedList<MyBidDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly SortMappingProvider _sortMappingProvider;

    public GetMyBidsQueryHandler(
        IDbContext dbContext,
        ICurrentUser currentUser,
        SortMappingProvider sortMappingProvider)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _sortMappingProvider = sortMappingProvider;
    }

    public async Task<Result<PagedList<MyBidDto>, Error>> Handle(
        GetMyBidsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;

        // Get the latest bid per auction for this bidder
        // (user may have multiple bids per auction, show only their latest)
        var query = _dbContext.Set<Bid>()
            .AsNoTracking()
            .Where(b => b.BidderId == _currentUser.UserId)
            .Join(
                _dbContext.Set<Auction>().AsNoTracking(),
                b => b.AuctionId,
                a => a.Id,
                (b, a) => new { Bid = b, Auction = a })
            .Join(
                _dbContext.Set<Item>().AsNoTracking(),
                x => x.Auction.ItemId,
                i => i.Id,
                (x, i) => new { x.Bid, x.Auction, Item = i });

        // Status filter
        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var bidStatus = BidStatus.FromId(parameters.Status);
            query = query.Where(x => x.Bid.Status == bidStatus);
        }
        
        var sortMapping = _sortMappingProvider.GetMappings<MyBidDto, Bid>();
        // Sorting
        //query.ApplySort(parameters, sortMapping);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Page(parameters)
            .Select(x => new MyBidDto(
                x.Bid.Id.Value,
                x.Auction.Id.Value,
                x.Item.Title,
                x.Item.Media
                    .Where(img => img.IsPrimary)
                    .Select(img => img.Url)
                    .FirstOrDefault(),
                x.Bid.Amount.Amount,
                x.Auction.CurrentPrice.Amount,
                x.Bid.Status.Id,
                x.Auction.Status.Id,
                x.Bid.Status == BidStatus.Winning,
                x.Bid.CreatedAt,
                x.Auction.Duration.EndTime))
            .ToListAsync(cancellationToken);

        return PagedList<MyBidDto>.ToPagedList(
            items, totalCount, parameters);
    }
}