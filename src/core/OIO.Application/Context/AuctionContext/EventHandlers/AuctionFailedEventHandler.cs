using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionFailedEventHandler
    : INotificationHandler<AuctionFailedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly IAuctionNotificationService _hubNotifier;
    private readonly ILogger<AuctionFailedEventHandler> _logger;

    public AuctionFailedEventHandler(
        IDbContext dbContext,
        ISender sender,
        IAuctionNotificationService hubNotifier,
        ILogger<AuctionFailedEventHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _hubNotifier = hubNotifier;
        _logger = logger;
    }

    public async Task Handle(AuctionFailedEvent notification, CancellationToken cancellationToken)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));
        var sellerId = UserId.From(Guid.Parse(notification.SellerId));

        var auction = await _dbContext.Set<Auction>()
            .AsNoTracking()
            .Include(a => a.Watchers)
            .Include(a => a.Item)
            .FirstOrDefaultAsync(a =>
                    a.Id == auctionId &&
                    a.Item.SellerId == sellerId,
                cancellationToken);

        if (auction is null)
            return;

        await _hubNotifier.NotifyAuctionEndedAsync(
            auctionId.Value,
            new AuctionEndedNotification(
                AuctionId: auctionId.Value,
                WinnerId: null,
                WinnerDisplayName: null,
                FinalPrice: notification.FinalPrice,
                TotalBids: notification.TotalBids,
                ReserveMet: false),
            cancellationToken);

        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: sellerId.Value,
                NotificationType: "auction",
                EventType: "auction_failed",
                Title: "Phien dau gia khong thanh cong",
                Message:
                    $"Phien dau gia \"{auction.Item.Title.Value}\" ket thuc khong thanh cong. " +
                    $"Ly do: {notification.Reason}. Gia cuoi: {NotificationDispatch.FormatAmount(notification.FinalPrice, notification.Currency)}.",
                Priority: NotificationPriority.High,
                EntityType: "Auction",
                EntityId: auctionId.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    auctionId = auctionId.Value,
                    itemId = auction.ItemId.Value,
                    reason = notification.Reason,
                    finalPrice = notification.FinalPrice,
                    currency = notification.Currency,
                    totalBids = notification.TotalBids
                })),
            cancellationToken);

        var watcherUserIds = auction.Watchers
            .Where(w => w.NotifyOnEnd && w.UserId != sellerId)
            .Select(w => w.UserId.Value)
            .Distinct()
            .ToList();

        foreach (var watcherUserId in watcherUserIds)
        {
            await NotificationDispatch.DispatchAsync(
                _sender,
                _logger,
                new CreateNotificationCommand(
                    UserId: watcherUserId,
                    NotificationType: "auction",
                    EventType: "auction_ended",
                    Title: "Phien dau gia da ket thuc",
                    Message:
                        $"Phien dau gia \"{auction.Item.Title.Value}\" da ket thuc nhung khong co nguoi thang. " +
                        $"Gia cuoi: {NotificationDispatch.FormatAmount(notification.FinalPrice, notification.Currency)}.",
                    Priority: NotificationPriority.Normal,
                    EntityType: "Auction",
                    EntityId: auctionId.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        auctionId = auctionId.Value,
                        itemId = auction.ItemId.Value,
                        hasWinner = false,
                        finalPrice = notification.FinalPrice,
                        currency = notification.Currency
                    })),
                cancellationToken);
        }

        // Notify all bidders that the auction failed (deposit will be refunded)
        var bidderIds = auction.Bids
            .Select(b => b.BidderId.Value)
            .Distinct()
            .Where(id => id != sellerId.Value)
            .ToList();

        foreach (var bidderId in bidderIds)
        {
            if (watcherUserIds.Contains(bidderId))
                continue;

            await NotificationDispatch.DispatchAsync(
                _sender,
                _logger,
                new CreateNotificationCommand(
                    UserId: bidderId,
                    NotificationType: "auction",
                    EventType: "auction_lost",
                    Title: "Phien dau gia da ket thuc",
                    Message:
                        $"Phien dau gia \"{auction.Item.Title.Value}\" da ket thuc khong co nguoi thang. " +
                        $"Tien dat coc se duoc hoan tra.",
                    Priority: NotificationPriority.Normal,
                    EntityType: "Auction",
                    EntityId: auctionId.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        auctionId = auctionId.Value,
                        reason = notification.Reason,
                        depositRefund = true
                    })),
                cancellationToken);
        }

        _logger.LogInformation(
            "AuctionFailed notifications created. Auction={AuctionId}, Reason={Reason}, Watchers={WatcherCount}, Bidders={BidderCount}.",
            notification.AuctionId, notification.Reason, watcherUserIds.Count, bidderIds.Count);
    }
}
