using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionEndedEventHandler
    : INotificationHandler<AuctionEndedEvent>
{
    private readonly IAuctionNotificationService _notificationService;
    private readonly ILogger<AuctionEndedEventHandler> _logger;

    public AuctionEndedEventHandler(
        IAuctionNotificationService notificationService,
        ILogger<AuctionEndedEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(AuctionEndedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Broadcasting auction ended: Auction={AuctionId}, Winner={WinnerId}",
            notification.AuctionId, notification.WinnerId);

        await _notificationService.NotifyAuctionEndedAsync(
            Guid.Parse(notification.AuctionId),
            new AuctionEndedNotification(
                AuctionId: Guid.Parse(notification.AuctionId),
                WinnerId: notification.WinnerId != null ? Guid.Parse(notification.WinnerId) : null,
                WinnerDisplayName: "",
                FinalPrice: notification.FinalPrice,
                TotalBids: notification.TotalBids,
                ReserveMet: notification.ReserveMet),
            ct);
    }
}