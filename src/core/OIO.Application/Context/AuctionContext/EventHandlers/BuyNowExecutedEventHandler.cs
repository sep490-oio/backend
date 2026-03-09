using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class BuyNowExecutedEventHandler
    : INotificationHandler<BuyNowExecutedEvent>
{
    private readonly IAuctionNotificationService _notificationService;
    private readonly ILogger<BuyNowExecutedEventHandler> _logger;

    public BuyNowExecutedEventHandler(
        IAuctionNotificationService notificationService,
        ILogger<BuyNowExecutedEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(BuyNowExecutedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Broadcasting buy now: Auction={AuctionId}, Buyer={BuyerId}",
            notification.AuctionId, notification.BuyerId);

        await _notificationService.NotifyBuyNowExecutedAsync(
            Guid.Parse(notification.AuctionId),
            new BuyNowNotification(
                AuctionId: Guid.Parse(notification.AuctionId),
                BuyerId: Guid.Parse(notification.BuyerId),
                Price: notification.BuyNowPrice),
            ct);
    }
}