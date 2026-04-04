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
using OIO.Domain.Context.AuctionContext.Errors;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
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

        var recentBids = auction.Bids
            .OrderByDescending(b => b.CreatedAt)
            .Take(20)
            .ToList();

        var bidderIds = recentBids
            .Select(b => b.BidderId)
            .Distinct()
            .Select(id => UserId.From(id.Value))
            .ToList();

        var bidderUsers = await _dbContext.Set<User>()
            .AsNoTracking()
            .Include(x => x.Profile)
            .Where(x => bidderIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

        var bidderDisplayNames = bidderUsers.ToDictionary(
            x => x.Id.Value,
            x => AuctionNotificationDisplayNames.Resolve(x));

        return new AuctionDetailDto(
            Auction: auction.ToDto(nowUtc, _runtimeSettings.Auction.ExtensionThreshold),
            Item: auction.Item.ToDto(),
            RecentBids: recentBids
                .Select(b => b.ToDto(bidderDisplayNames.TryGetValue(b.BidderId.Value, out var dn) ? dn : null))
                .ToList(),
            PriceHistory: auction.PriceHistories
                .OrderByDescending(ph => ph.CreatedAt)
                .Select(ph => ph.ToDto())
                .ToList(),
            CurrentUserParticipant: currentUserParticipant);
    }
}

