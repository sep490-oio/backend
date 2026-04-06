using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionCancelledEventHandler
    : INotificationHandler<AuctionCancelledEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly ILogger<AuctionCancelledEventHandler> _logger;

    public AuctionCancelledEventHandler(
        IDbContext dbContext,
        ISender sender,
        ILogger<AuctionCancelledEventHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
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

        var allUserIds = auction.Bids
            .Select(b => b.BidderId)
            .Union(auction.Watchers.Select(w => w.UserId))
            .Where(id => id != auction.Item.SellerId)
            .Distinct()
            .Select(id => id.Value)
            .ToList();

        if (allUserIds.Count == 0)
        {
            _logger.LogInformation(
                "Auction cancelled. Id={AuctionId}, Reason={Reason}, Notified={Count} users via notification engine.",
                notification.AuctionId, notification.Reason, 0);
            return;
        }

        foreach (var userId in allUserIds)
        {
            await NotificationDispatch.DispatchAsync(
                _sender,
                _logger,
                new CreateNotificationCommand(
                    UserId: userId,
                    NotificationType: "auction",
                    EventType: "auction_cancelled",
                    Title: "Phien dau gia da bi huy",
                    Message: $"Phien dau gia \"{auction.Item.Title.Value}\" da bi huy. Ly do: {notification.Reason}",
                    Priority: NotificationPriority.High,
                    EntityType: "Auction",
                    EntityId: auctionId.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        auctionId = auctionId.Value,
                        itemId = auction.ItemId.Value,
                        reason = notification.Reason
                    })),
                cancellationToken);
        }

        _logger.LogInformation(
            "Auction cancelled. Id={AuctionId}, Reason={Reason}, Notified={Count} users via notification engine.",
            notification.AuctionId, notification.Reason, allUserIds.Count);
    }
}
