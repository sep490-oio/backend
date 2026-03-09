using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class BidPlacedEventHandler
    : INotificationHandler<BidPlacedEvent>
{
    private readonly IAuctionNotificationService _notificationService;
    private readonly IClock _clock;
    private readonly ILogger<BidPlacedEventHandler> _logger;

    public BidPlacedEventHandler(
        IAuctionNotificationService notificationService,
        IClock clock,
        ILogger<BidPlacedEventHandler> logger)
    {
        _notificationService = notificationService;
        _clock = clock;
        _logger = logger;
    }

    public async Task Handle(BidPlacedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Broadcasting bid: Auction={AuctionId}, Bidder={BidderId}, Amount={Amount}",
            notification.AuctionId, notification.BidderId, notification.Amount);

        await _notificationService.NotifyBidPlacedAsync(
            Guid.Parse(notification.AuctionId),
            new BidNotification(
                AuctionId: Guid.Parse(notification.AuctionId),
                BidId: Guid.Parse(notification.BidId),
                BidderId: Guid.Parse(notification.BidderId),
                BidderDisplayName: "",
                Amount: notification.Amount,
                CurrentPrice: notification.Amount,
                MinimumNextBid: 0,
                TotalBids: 0,
                IsAutoBid: notification.IsAutoBid,
                Timestamp: _clock.UtcNow),
            ct);
    }
}