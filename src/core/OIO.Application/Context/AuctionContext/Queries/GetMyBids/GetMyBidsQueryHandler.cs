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
        var userId = _currentUser.UserId;

        // Get the latest bid per auction for this bidder
        // Uses NOT EXISTS pattern instead of GroupBy+First() which EF Core can't translate
        var query = _dbContext.Set<Bid>()
            .AsNoTracking()
            .Where(bid => bid.BidderId == userId)
            .Where(bid => !_dbContext.Set<Bid>()
                .Any(newer => newer.BidderId == userId
                    && newer.AuctionId == bid.AuctionId
                    && newer.CreatedAt > bid.CreatedAt));

        // Position filter — map position string to query predicate
        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var pos = parameters.Status.ToLowerInvariant();
            query = pos switch
            {
                // IsSuccessfullyClosed expansion — EF translation requires the inlined predicate.
                "won"     => query.Where(b => (b.Auction.Status == AuctionStatus.Sold
                                              || b.Auction.Status == AuctionStatus.Completed)
                                           && b.Auction.WinnerId == userId),
                "lost"    => query.Where(b => (b.Auction.Status == AuctionStatus.Ended
                                              || b.Auction.Status == AuctionStatus.Failed
                                              || b.Auction.Status == AuctionStatus.Cancelled
                                              || b.Auction.Status == AuctionStatus.Terminated
                                              || b.Auction.Status == AuctionStatus.PaymentDefaulted)
                                           && b.Auction.WinnerId != userId),
                "leading" => query.Where(b => b.Status == BidStatus.Winning),
                "outbid"  => query.Where(b => b.Status == BidStatus.Outbid),
                _         => query,
            };
        }

        query = query.ApplySort(parameters, BidMappings.MyBidDtoSortMapping);

        var totalCount = await query.CountAsync(cancellationToken);

        var myBids = await query
            .Select(x => new MyBidDto(
                x.Auction.Id.Value,
                x.Auction.Item.Id.Value,
                x.Auction.Item.Title.Value,
                x.Auction.Item.Media
                    .Where(img => img.IsPrimary)
                    .Select(img => img.Info.SecureUrl)
                    .FirstOrDefault(),
                x.Auction.Status.Id,
                x.Auction.Pricing.CurrentPrice.ToDto(),
                x.Amount.ToDto(),
                // Compute position
                // IsSuccessfullyClosed expansion — EF translation requires the inlined predicate.
                (x.Auction.Status == AuctionStatus.Sold || x.Auction.Status == AuctionStatus.Completed)
                  && x.Auction.WinnerId == userId
                    ? "won"
                    : (x.Auction.Status == AuctionStatus.Ended
                       || x.Auction.Status == AuctionStatus.Failed
                       || x.Auction.Status == AuctionStatus.Cancelled
                       || x.Auction.Status == AuctionStatus.Terminated
                       || x.Auction.Status == AuctionStatus.PaymentDefaulted)
                        ? "lost"
                        : x.Status == BidStatus.Winning
                            ? "leading"
                            : "outbid",
                // WonAt — use auction's sold/closed timestamp; not directly available, use null
                (DateTime?)null,
                x.CreatedAt,
                _dbContext.Set<Bid>().Count(b => b.BidderId == userId && b.AuctionId == x.AuctionId)))
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        // Enrich won bids with their corresponding order (for direct-pay from
        // /me/bids → /checkout/{orderId}). Batch-load in one query to stay
        // N+1 safe.
        var wonAuctionIds = myBids.Items
            .Where(b => b.Position == "won")
            .Select(b => OIO.Domain.Context.AuctionContext.ValueObjects.Ids.AuctionId.From(b.AuctionId))
            .Distinct()
            .ToList();

        if (wonAuctionIds.Count > 0)
        {
            var orders = await _dbContext.Set<OIO.Domain.Context.OrderContext.Aggregates.Orders.Order>()
                .AsNoTracking()
                .Where(o => o.BuyerId == userId && wonAuctionIds.Contains(o.AuctionId))
                .Select(o => new { o.Id, o.AuctionId, o.Status })
                .ToListAsync(cancellationToken);

            var orderByAuctionId = orders.ToDictionary(o => o.AuctionId, o => o);

            var enrichedItems = myBids.Items
                .Select(b =>
                {
                    if (b.Position != "won") return b;
                    var auctionId = OIO.Domain.Context.AuctionContext.ValueObjects.Ids.AuctionId.From(b.AuctionId);
                    if (!orderByAuctionId.TryGetValue(auctionId, out var order)) return b;
                    var statusId = order.Status.Id;
                    var canPayNow = statusId == "pending_payment";
                    return b with
                    {
                        OrderId = order.Id.Value,
                        OrderStatus = statusId,
                        CanPayNow = canPayNow,
                    };
                })
                .ToList();

            return new PagedList<MyBidDto>(enrichedItems, myBids.Metadata.TotalCount, myBids.Metadata.CurrentPage, myBids.Metadata.PageSize);
        }

        return myBids;
    }
}