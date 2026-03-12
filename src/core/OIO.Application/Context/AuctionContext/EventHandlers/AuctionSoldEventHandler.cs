using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Mail;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionSoldEventHandler
    : INotificationHandler<AuctionSoldEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IUserMailNotifier _mailNotifier;
    private readonly IAuctionNotificationService _hubNotifier;
    private readonly ILogger<AuctionSoldEventHandler> _logger;

    public AuctionSoldEventHandler(
        IDbContext dbContext,
        IUserMailNotifier mailNotifier,
        IAuctionNotificationService hubNotifier,
        ILogger<AuctionSoldEventHandler> logger)
    {
        _dbContext = dbContext;
        _mailNotifier = mailNotifier;
        _hubNotifier = hubNotifier;
        _logger = logger;
    }

    public async Task Handle(AuctionSoldEvent notification, CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));
        var winnerId = UserId.From(Guid.Parse(notification.WinnerId));
        var sellerId = UserId.From(Guid.Parse(notification.SellerId));

        // Load auction with item for title
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(a => a.Watchers)
                .Include(a => a.Item),
            cancellationToken: cancellationToken
        );

        // Load winner + seller
        var winner = await _dbContext.GetByIdAsync<User, UserId>(
            id: winnerId,
            queryBuilder: query => query
                .AsNoTracking(),
            cancellationToken: cancellationToken
        );
            
        var seller =  await _dbContext.GetByIdAsync<User, UserId>(
            id: sellerId,
            queryBuilder: query => query
                .AsNoTracking(),
            cancellationToken: cancellationToken
        );

        if (auction is null || winner is null || seller is null)
        {
            _logger.LogWarning("AuctionSoldEventHandler: missing data for auction {AuctionId}.",
                notification.AuctionId);
            return;
        }
        
        var auctionTitle = $"Auction #{notification.AuctionId[..8]} {auction.Item.Title}";

        // 1. Email winner
        await _mailNotifier.SendAuctionWonAsync(
            toEmail: winner.Email.Value,
            userName: winner.UserName.Value,
            auctionId: notification.AuctionId,
            auctionTitle: auctionTitle,
            finalPrice: notification.FinalPrice,
            currency: notification.Currency,
            paymentUrl: "https://example.com/payment", // TODO: generate real payment URL
            cancellationToken: cancellationToken);

        // 2. Email seller
        await _mailNotifier.SendAuctionSoldAsync(
            toEmail: seller.Email.Value,
            userName: seller.UserName.Value,
            auctionId: notification.AuctionId,
            auctionTitle: auctionTitle,
            winnerName: winner.UserName.Value,
            finalPrice: notification.FinalPrice,
            currency: notification.Currency,
            cancellationToken: cancellationToken);

        // 3. SignalR broadcast to auction room
        await _hubNotifier.NotifyAuctionEndedAsync(
            auctionId.Value,
            new AuctionEndedNotification(
                AuctionId: auctionId.Value,
                WinnerId: winnerId.Value,
                WinnerDisplayName: winner.UserName.Value,
                FinalPrice: notification.FinalPrice,
                TotalBids: notification.TotalBids,
                ReserveMet: true),
            cancellationToken: cancellationToken);

        // 4. Email watchers (who opted in for notifyOnEnd)
        var watcherUserIds = auction.Watchers
            .Where(w => w.NotifyOnEnd && w.UserId != winnerId && w.UserId != sellerId)
            .Select(w => w.UserId)
            .ToList();

        if (watcherUserIds.Count > 0)
        {
            var watchers = await _dbContext.Set<User>()
                .AsNoTracking()
                .Where(u => watcherUserIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            foreach (var watcher in watchers)
            {
                await _mailNotifier.SendAuctionEndedWatcherAsync(
                    toEmail: watcher.Email.Value,
                    userName: watcher.UserName.Value,
                    auctionId: notification.AuctionId,
                    auctionTitle: auctionTitle,
                    finalPrice: notification.FinalPrice,
                    currency: notification.Currency,
                    hasWinner: true,
                    cancellationToken);
            }
        }

        _logger.LogInformation(
            "✅ AuctionSold notifications sent. Auction={AuctionId}, Winner={WinnerId}, Price={Price}.",
            notification.AuctionId, notification.WinnerId, notification.FinalPrice);
    }
}