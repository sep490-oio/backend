using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Application.Context.AuctionContext.EventHandlers;
using OIO.Application.Context.AuctionContext.Mappings;
using OIO.Application.Context.UserContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.OrderContext.Aggregates.Orders;
using OIO.Domain.Context.OrderContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.AuctionContext.Queries.GetAuctionById;

internal sealed class GetAuctionByIdQueryHandler
    : IQueryHandler<GetAuctionByIdQuery, AuctionDetailDto>
{
    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IRuntimeSettings _runtimeSettings;
    private readonly ICurrentUser _currentUser;

    public GetAuctionByIdQueryHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IClock clock,
        IRuntimeSettings runtimeSettings,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _runtimeSettings = runtimeSettings;
        _currentUser = currentUser;
    }

    public async Task<Result<AuctionDetailDto, Error>> Handle(
        GetAuctionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(request.AuctionId);
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .Include(a => a.Bids.OrderByDescending(b => b.CreatedAt))
                .Include(a => a.AutoBids)
                .Include(a => a.Watchers)
                .Include(a => a.BuyNowReservations)
                .Include(a => a.SealedBids)
                .Include(a => a.PriceHistories.OrderByDescending(ph => ph.CreatedAt))
                .Include(x => x.Item)
                .ThenInclude(x => x.Media)
                .AsSplitQuery(),
            cancellationToken: cancellationToken);

        if (auction is null)
            return AuctionErrors.Auction.NotFound(auctionId);

        var nowUtc = _clock.UtcNow;

        // View count increment moved to RecordAuctionViewCommand (CQRS compliance)

        ParticipantInfoDto? currentUserParticipant = null;

        if (_currentUser.IsAuthenticated)
        {
            var userId = _currentUser.UserId;

            var participant = await _dbContext.Set<AuctionParticipant>()
                .AsNoTracking()
                .Where(p => p.AuctionId == auctionId && p.UserId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            var deposit = await _dbContext.Set<AuctionDeposit>()
                .AsNoTracking()
                .Where(d => d.AuctionId == auctionId && d.BidderId == userId)
                .FirstOrDefaultAsync(cancellationToken);

            if (participant is not null || deposit is not null)
            {
                currentUserParticipant = new ParticipantInfoDto(
                    QualificationStatus: participant?.QualificationStatus.ToString(),
                    JoinStatus: participant?.JoinStatus.ToString(),
                    DepositStatus: deposit?.Status.ToString(),
                    DepositAmount: deposit?.Amount.Amount,
                    DepositCurrency: deposit?.Amount.Currency.Id);
            }
        }

        CurrentUserBidStateDto? currentUserBidState = null;

        if (_currentUser.IsAuthenticated)
        {
            var userId = _currentUser.UserId;

            // Find user's latest bid
            var latestBid = auction.Bids
                .Where(b => b.BidderId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefault();

            // Find user's auto-bid
            var autoBid = auction.AutoBids
                .FirstOrDefault(ab => ab.BidderId == userId);

            // Determine position
            var winningBid = auction.GetCurrentWinningBid();
            var isCurrentWinner = winningBid?.BidderId == userId;

            string position;
            var auctionStatus = auction.Status.Id.ToLowerInvariant();
            if (auctionStatus is "ended" or "sold")
            {
                position = auction.WinnerId == userId ? "won" : (latestBid != null ? "lost" : "none");
            }
            else if (auctionStatus is "failed" or "cancelled" or "terminated")
            {
                position = latestBid != null ? "lost" : "none";
            }
            else if (isCurrentWinner)
            {
                position = "leading";
            }
            else if (latestBid != null)
            {
                position = "outbid";
            }
            else
            {
                position = "none";
            }

            if (latestBid != null || autoBid != null)
            {
                currentUserBidState = new CurrentUserBidStateDto(
                    Position: position,
                    IsCurrentWinner: isCurrentWinner,
                    LatestBidId: latestBid?.Id.Value,
                    LatestBidAmount: latestBid?.Amount.Amount,
                    LatestBidStatus: latestBid?.Status.Id,
                    LatestBidAt: latestBid?.CreatedAt,
                    HasAutoBid: autoBid != null,
                    AutoBidStatus: autoBid?.Status.Id);
            }
        }

        var recentBids = auction.Bids
            .OrderByDescending(b => b.CreatedAt)
            .Take(20)
            .ToList();

        // Collect bidder IDs from both bids and price history entries
        var bidIdsFromHistory = auction.PriceHistories
            .Where(ph => ph.BidId is not null)
            .Select(ph => ph.BidId!.Value)
            .Distinct()
            .ToHashSet();

        var bidsById = auction.Bids
            .Where(b => bidIdsFromHistory.Contains(b.Id))
            .ToDictionary(b => b.Id.Value, b => b.BidderId.Value);

        var bidderIds = recentBids
            .Select(b => b.BidderId)
            .Distinct()
            .Select(id => UserId.From(id.Value))
            .Union(bidsById.Values.Distinct().Select(id => UserId.From(id)))
            .Distinct()
            .ToList();

        var bidderUsers = await _dbContext.Set<User>()
            .AsNoTracking()
            .Include(x => x.Profile)
            .Where(x => bidderIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var bidderDisplayNames = bidderUsers.ToDictionary(
            x => x.Id.Value,
            x => AuctionNotificationDisplayNames.Resolve(x));

        CurrentBuyerOrderDto? currentBuyerOrder = null;

        if (_currentUser.IsAuthenticated)
        {
            var userId = _currentUser.UserId;
            var isAuctionWinner = auction.WinnerId == userId;
            var isBuyNowWinner = auction.BuyNowReservations.Any(r => r.BuyerId == userId);

            if (isAuctionWinner || isBuyNowWinner)
            {
                var order = await _dbContext.Set<Order>()
                    .AsNoTracking()
                    .Where(o => o.AuctionId == auctionId && o.BuyerId == userId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (order is not null)
                {
                    currentBuyerOrder = new CurrentBuyerOrderDto(
                        OrderId: order.Id.Value,
                        OrderStatus: order.Status.Id,
                        CanPayNow: order.Status == OrderStatus.PendingPayment);
                }
            }
        }

        // ── Sealed bid info ────────────────────────────────────────────
        SealedBidInfoDto? sealedBidInfo = null;

        if (auction.AuctionType == AuctionType.Sealed)
        {
            var sealedBidCount = auction.SealedBids.Count;
            var hasSubmitted = false;
            string? userSealedBidStatus = null;

            if (_currentUser.IsAuthenticated)
            {
                var userSealedBid = auction.SealedBids
                    .FirstOrDefault(sb => sb.BidderId == _currentUser.UserId);

                if (userSealedBid is not null)
                {
                    hasSubmitted = true;
                    userSealedBidStatus = userSealedBid.Status.Id;
                }
            }

            sealedBidInfo = new SealedBidInfoDto(
                SealedBidCount: sealedBidCount,
                CurrentUserHasSubmittedSealedBid: hasSubmitted,
                CurrentUserSealedBidStatus: userSealedBidStatus);
        }

        return new AuctionDetailDto(
            Auction: auction.ToDto(
                nowUtc, 
                _runtimeSettings.Auction.ExtensionThreshold,
                _currentUser.IsAuthenticated && auction.Watchers.Any(w => w.UserId == _currentUser.UserId)),
            Item: auction.Item.ToDto(),
            RecentBids: recentBids
                .Select(b => b.ToDto(bidderDisplayNames.TryGetValue(b.BidderId.Value, out var dn) ? dn : null))
                .ToList(),
            PriceHistory: auction.PriceHistories
                .OrderByDescending(ph => ph.CreatedAt)
                .Select(ph =>
                {
                    string? displayName = null;
                    if (ph.BidId is not null &&
                        bidsById.TryGetValue(ph.BidId.Value.Value, out var bidderId) &&
                        bidderDisplayNames.TryGetValue(bidderId, out var dn))
                    {
                        displayName = dn;
                    }
                    return ph.ToDto(displayName);
                })
                .ToList(),
            CurrentUserParticipant: currentUserParticipant,
            CurrentUserBidState: currentUserBidState,
            CurrentBuyerOrder: currentBuyerOrder,
            SealedBidInfo: sealedBidInfo);
    }
}

