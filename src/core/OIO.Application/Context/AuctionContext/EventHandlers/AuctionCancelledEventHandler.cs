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

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionCancelledEventHandler
    : INotificationHandler<AuctionCancelledEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IUserMailNotifier _mailNotifier;
    private readonly IAuctionNotificationService _hubNotifier;
    private readonly ILogger<AuctionCancelledEventHandler> _logger;

    public AuctionCancelledEventHandler(
        IDbContext dbContext,
        IUserMailNotifier mailNotifier,
        IAuctionNotificationService hubNotifier,
        ILogger<AuctionCancelledEventHandler> logger)
    {
        _dbContext = dbContext;
        _mailNotifier = mailNotifier;
        _hubNotifier = hubNotifier;
        _logger = logger;
    }

    public async Task Handle(AuctionCancelledEvent notification, CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            id: auctionId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(a => a.Bids)
                .Include(a => a.Watchers)
                .Include(a => a.Item),
            cancellationToken: cancellationToken);

        if (auction is null) 
            return;

        // Item title for email (load separately or from auction navigation)
        var auctionTitle = $"Auction #{notification.AuctionId[..8]}";

        // 1. SignalR broadcast
        await _hubNotifier.NotifyAuctionCancelledAsync(
            auctionId.Value,
            new AuctionCancelledNotification(
                AuctionId: auctionId.Value,
                Reason: notification.Reason),
            cancellationToken);

        // 2. Collect unique user IDs: bidders + watchers (exclude seller)
        var bidderIds = auction.Bids
            .Select(b => b.BidderId)
            .Distinct()
            .ToList();

        var watcherIds = auction.Watchers
            .Select(w => w.UserId)
            .ToList();

        var allUserIds = bidderIds
            .Union(watcherIds)
            .Where(id => id != auction.Item.SellerId)
            .Distinct()
            .ToList();

        if (allUserIds.Count == 0) 
            return;

        var users = await _dbContext.Set<User>()
            .AsNoTracking()
            .Where(u => allUserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        // 3. Email each affected user
        foreach (var user in users)
        {
            await _mailNotifier.SendAuctionCancelledAsync(
                toEmail: user.Email.Value,
                userName: user.UserName.Value,
                auctionId: notification.AuctionId,
                auctionTitle: auctionTitle,
                reason: notification.Reason,
                cancellationToken);
        }

        _logger.LogInformation(
            "Auction cancelled. Id={AuctionId}, Reason={Reason}, Notified={Count} users.",
            notification.AuctionId, notification.Reason, users.Count);
    }
}