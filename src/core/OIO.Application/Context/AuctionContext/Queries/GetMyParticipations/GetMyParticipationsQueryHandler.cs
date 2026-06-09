using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.UserContext.Services;
using OIO.Application.Extensions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetMyParticipations;

internal sealed class GetMyParticipationsQueryHandler
    : IQueryHandler<GetMyParticipationsQuery, PagedList<MyParticipationDto>>
{
    private readonly IDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetMyParticipationsQueryHandler(IDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedList<MyParticipationDto>, Error>> Handle(
        GetMyParticipationsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters = request.Parameters;
        var userId = _currentUser.UserId;

        // ── Base query: start from AuctionDeposit (every participant deposited) ──
        // LEFT JOIN with the user's latest bid per auction.
        var query = _dbContext.Set<AuctionDeposit>()
            .AsNoTracking()
            .Where(d => d.BidderId == userId);

        // ── Status filters ──
        if (!string.IsNullOrWhiteSpace(parameters.Status))
        {
            var status = parameters.Status.ToLowerInvariant();
            query = status switch
            {
                "active" => query.Where(d =>
                    d.Auction.Status == AuctionStatus.Active
                    && _dbContext.Set<Bid>().Any(b =>
                        b.BidderId == userId
                        && b.AuctionId == d.AuctionId)),

                // Leading: auction still running and the user's latest bid is currently winning.
                "leading" => query.Where(d =>
                    d.Auction.Status == AuctionStatus.Active
                    && _dbContext.Set<Bid>()
                        .Where(b => b.BidderId == userId && b.AuctionId == d.AuctionId)
                        .OrderByDescending(b => b.CreatedAt)
                        .Select(b => b.Status.Id)
                        .FirstOrDefault() == BidStatus.Winning.Id),

                // Outbid: auction still running, the user has bid, but their latest bid is no longer winning.
                "outbid" => query.Where(d =>
                    d.Auction.Status == AuctionStatus.Active
                    && _dbContext.Set<Bid>().Any(b =>
                        b.BidderId == userId
                        && b.AuctionId == d.AuctionId)
                    && _dbContext.Set<Bid>()
                        .Where(b => b.BidderId == userId && b.AuctionId == d.AuctionId)
                        .OrderByDescending(b => b.CreatedAt)
                        .Select(b => b.Status.Id)
                        .FirstOrDefault() != BidStatus.Winning.Id),

                "won" => query.Where(d =>
                    (d.Auction.Status == AuctionStatus.Sold
                     || d.Auction.Status == AuctionStatus.Completed)
                    && d.Auction.WinnerId == userId),

                "lost" => query.Where(d =>
                    (d.Auction.Status == AuctionStatus.Ended
                     || d.Auction.Status == AuctionStatus.Failed
                     || d.Auction.Status == AuctionStatus.Cancelled
                     || d.Auction.Status == AuctionStatus.Terminated
                     || d.Auction.Status == AuctionStatus.PaymentDefaulted)
                    && d.Auction.WinnerId != userId),

                "deposit_only" => query.Where(d =>
                    !_dbContext.Set<Bid>().Any(b =>
                        b.BidderId == userId
                        && b.AuctionId == d.AuctionId)),

                _ => query, // "all"
            };
        }
        
        // ── Search ──
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var searchTerm = parameters.Search.ToLower();
            query = query.Where(d => 
                d.Auction.Item.Title.Value.ToLower().Contains(searchTerm) || 
                d.AuctionId.Value.ToString().ToLower().Contains(searchTerm));
        }

        // Default sort: newest deposit first
        query = query.OrderByDescending(d => d.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        // ── Project ──
        var items = await query
            .Select(d => new MyParticipationDto(
                d.AuctionId.Value,
                d.Auction.Item.Id.Value,
                d.Auction.Item.Title.Value,
                d.Auction.Item.Media
                    .Where(img => img.IsPrimary)
                    .Select(img => img.Info.SecureUrl)
                    .FirstOrDefault(),
                d.Auction.Status.Id,
                new MoneyDto(
                    d.Auction.Pricing.CurrentPrice.Amount,
                    d.Auction.Pricing.CurrentPrice.Currency.Id,
                    d.Auction.Pricing.CurrentPrice.Currency.Symbol),
                // Deposit fields
                d.Amount.Amount,
                d.Amount.Currency.Id,
                d.Status.Id,
                d.CreatedAt,
                // Bid fields — use subquery for latest bid
                _dbContext.Set<Bid>()
                    .Where(b => b.BidderId == userId && b.AuctionId == d.AuctionId)
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => (MoneyDto?)new MoneyDto(
                        b.Amount.Amount,
                        b.Amount.Currency.Id,
                        b.Amount.Currency.Symbol))
                    .FirstOrDefault(),
                // BidPosition — computed inline
                _dbContext.Set<Bid>()
                    .Where(b => b.BidderId == userId && b.AuctionId == d.AuctionId)
                    .Any()
                    ? (d.Auction.Status == AuctionStatus.Sold || d.Auction.Status == AuctionStatus.Completed)
                        && d.Auction.WinnerId == userId
                        ? "won"
                        : (d.Auction.Status == AuctionStatus.Ended
                           || d.Auction.Status == AuctionStatus.Failed
                           || d.Auction.Status == AuctionStatus.Cancelled
                           || d.Auction.Status == AuctionStatus.Terminated
                           || d.Auction.Status == AuctionStatus.PaymentDefaulted)
                            ? "lost"
                            : _dbContext.Set<Bid>()
                                .Where(b => b.BidderId == userId && b.AuctionId == d.AuctionId)
                                .OrderByDescending(b => b.CreatedAt)
                                .Select(b => b.Status.Id)
                                .FirstOrDefault() == BidStatus.Winning.Id
                                ? "leading"
                                : "outbid"
                    : (string?)null,
                // LastBidAt
                _dbContext.Set<Bid>()
                    .Where(b => b.BidderId == userId && b.AuctionId == d.AuctionId)
                    .OrderByDescending(b => b.CreatedAt)
                    .Select(b => (DateTime?)b.CreatedAt)
                    .FirstOrDefault(),
                // BidCountForUser
                _dbContext.Set<Bid>()
                    .Count(b => b.BidderId == userId && b.AuctionId == d.AuctionId)))
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        // ── Enrich won entries with order info ──
        var wonAuctionIds = items.Items
            .Where(p => p.BidPosition == "won")
            .Select(p => AuctionId.From(p.AuctionId))
            .Distinct()
            .ToList();

        if (wonAuctionIds.Count > 0)
        {
            var orders = await _dbContext
                .Set<OIO.Domain.Context.OrderContext.Aggregates.Orders.Order>()
                .AsNoTracking()
                .Where(o => o.BuyerId == userId && wonAuctionIds.Contains(o.AuctionId))
                .Select(o => new { o.Id, o.AuctionId, o.Status })
                .ToListAsync(cancellationToken);

            var orderByAuction = orders
                .GroupBy(o => o.AuctionId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(o => o.Status.Id != "cancelled")
                          .ThenByDescending(o => o.Id)
                          .First());

            var enriched = items.Items
                .Select(p =>
                {
                    if (p.BidPosition != "won") return p;
                    var aid = AuctionId.From(p.AuctionId);
                    if (!orderByAuction.TryGetValue(aid, out var order)) return p;
                    return p with
                    {
                        OrderId = order.Id.Value,
                        OrderStatus = order.Status.Id,
                        CanPayNow = order.Status.Id == "pending_payment",
                    };
                })
                .ToList();

            return new PagedList<MyParticipationDto>(
                enriched, items.Metadata.TotalCount,
                items.Metadata.CurrentPage, items.Metadata.PageSize);
        }

        return items;
    }
}
