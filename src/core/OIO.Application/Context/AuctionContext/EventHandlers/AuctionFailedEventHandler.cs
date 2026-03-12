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

internal sealed class AuctionFailedEventHandler
    : INotificationHandler<AuctionFailedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IUserMailNotifier _mailNotifier;
    private readonly IAuctionNotificationService _hubNotifier;
    private readonly ILogger<AuctionFailedEventHandler> _logger;

    public AuctionFailedEventHandler(
        IDbContext dbContext,
        IUserMailNotifier mailNotifier,
        IAuctionNotificationService hubNotifier,
        ILogger<AuctionFailedEventHandler> logger)
    {
        _dbContext = dbContext;
        _mailNotifier = mailNotifier;
        _hubNotifier = hubNotifier;
        _logger = logger;
    }

    public async Task Handle(AuctionFailedEvent notification, CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));
        var sellerId = UserId.From(Guid.Parse(notification.SellerId));

        var seller = await _dbContext.GetByIdAsync<User, UserId>(
            id: sellerId,
            queryBuilder: query => query.AsNoTracking(),
            cancellationToken: cancellationToken
        );

        if (seller is null) 
            return;
        
        var auction = await _dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Watchers)
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a => 
                    a.Id == auctionId && 
                    a.Item.SellerId == sellerId, 
                cancellationToken: cancellationToken);

        if (auction is null)
        {
            return;
        }

        // 1. SignalR broadcast
        await _hubNotifier.NotifyAuctionEndedAsync(
            auctionId.Value,
            new AuctionEndedNotification(
                AuctionId: auctionId.Value,
                WinnerId: null,
                WinnerDisplayName: null,
                FinalPrice: notification.FinalPrice,
                TotalBids: notification.TotalBids,
                ReserveMet: false),
            cancellationToken: cancellationToken
        );

        
        var auctionTitle = $"Auction #{notification.AuctionId[..8]} {auction.Item.Title}"; // Load item title if needed

        // 2. Email seller
        await _mailNotifier.SendAuctionFailedAsync(
            toEmail: seller.Email.Value,
            userName: seller.UserName.Value,
            auctionId: notification.AuctionId,
            auctionTitle: auctionTitle,
            reason: notification.Reason,
            finalPrice: notification.FinalPrice,
            totalBids: notification.TotalBids,
            currency: notification.Currency,
            cancellationToken: cancellationToken
        );

        // 3. Notify watchers
        var watcherUserIds = auction.Watchers
            .Where(w => w.NotifyOnEnd && w.UserId != sellerId)
            .Select(w => w.UserId)
            .ToList();

        if (watcherUserIds.Count > 0)
        {
            var watchers = await _dbContext.Set<User>()
                .AsNoTracking()
                .Where(u => watcherUserIds.Contains(u.Id))
                .ToListAsync(cancellationToken: cancellationToken
                );

            foreach (var watcher in watchers)
            {
                await _mailNotifier.SendAuctionEndedWatcherAsync(
                    toEmail: watcher.Email.Value,
                    userName: watcher.UserName.Value,
                    auctionId: notification.AuctionId,
                    auctionTitle: auctionTitle,
                    finalPrice: notification.FinalPrice,
                    currency: "VND",
                    hasWinner: false,
                    cancellationToken: cancellationToken
                );
            }
        }

        _logger.LogInformation(
            "AuctionFailed notifications sent. Auction={AuctionId}, Reason={Reason}.",
            notification.AuctionId, notification.Reason);
    }
}