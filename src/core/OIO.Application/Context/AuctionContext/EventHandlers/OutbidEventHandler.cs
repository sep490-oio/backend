using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class OutbidEventHandler
    : INotificationHandler<OutbidEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IAuctionNotificationService _notificationService;
    private readonly ILogger<OutbidEventHandler> _logger;

    public OutbidEventHandler(
        IDbContext dbContext,
        IAuctionNotificationService notificationService,
        ILogger<OutbidEventHandler> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(OutbidEvent notification, CancellationToken ct)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            auctionId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(a => a.Bids),
            cancellationToken: ct);

        if (auction is null)
        {
            _logger.LogWarning(
                "Skipped outbid notification because auction {AuctionId} could not be loaded.",
                notification.AuctionId);
            return;
        }

        var highBidderId = auction.GetCurrentWinningBid()?.BidderId ??
                           UserId.From(Guid.Parse(notification.NewHighBidderId));

        var highBidder = await _dbContext.GetByIdAsync<User, UserId>(
            highBidderId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(u => u.Profile),
            cancellationToken: ct);

        _logger.LogInformation(
            "Notifying outbid: Auction={AuctionId}, OutbidUser={OutbidUser}",
            notification.AuctionId, notification.OutbidBidderId);

        await _notificationService.NotifyOutbidAsync(
            Guid.Parse(notification.OutbidBidderId),
            new OutbidNotification(
                AuctionId: Guid.Parse(notification.AuctionId),
                NewHighAmount: auction.Pricing.CurrentAmount,
                MinimumNextBid: auction.GetMinimumBidAmount().Amount,
                NewHighBidderDisplayName: AuctionNotificationDisplayNames.Resolve(highBidder)),
            ct);
    }
}
