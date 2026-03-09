using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class OutbidEventHandler
    : INotificationHandler<OutbidEvent>
{
    private readonly IAuctionNotificationService _notificationService;
    private readonly ILogger<OutbidEventHandler> _logger;

    public OutbidEventHandler(
        IAuctionNotificationService notificationService,
        ILogger<OutbidEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(OutbidEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Notifying outbid: Auction={AuctionId}, OutbidUser={OutbidUser}",
            notification.AuctionId, notification.OutbidBidderId);

        await _notificationService.NotifyOutbidAsync(
            Guid.Parse(notification.OutbidBidderId),
            new OutbidNotification(
                AuctionId: Guid.Parse(notification.AuctionId),
                NewHighAmount: notification.NewHighestBid,
                MinimumNextBid: 0,
                NewHighBidderDisplayName: ""),
            ct);
    }
}