using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Scheduling;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionExtendedEventHandler
    : INotificationHandler<AuctionExtendedEvent>
{
    private readonly IAuctionNotificationService _notificationService;
    private readonly IAuctionScheduler _scheduler;
    private readonly ILogger<AuctionExtendedEventHandler> _logger;

    public AuctionExtendedEventHandler(
        IAuctionNotificationService notificationService,
        IAuctionScheduler scheduler,
        ILogger<AuctionExtendedEventHandler> logger)
    {
        _notificationService = notificationService;
        _scheduler = scheduler;
        _logger = logger;
    }

    public async Task Handle(AuctionExtendedEvent notification, CancellationToken ct)
    {
        await _scheduler.RescheduleEndAsync(Guid.Parse(notification.AuctionId), notification.NewEndTime, ct);

        await _notificationService.NotifyAuctionExtendedAsync(
            Guid.Parse(notification.AuctionId),
            new AuctionExtendedNotification(
                AuctionId: Guid.Parse(notification.AuctionId),
                NewEndTime: notification.NewEndTime,
                ExtensionMinutes: notification.ExtensionMinutes),
            ct);
        
        _logger.LogInformation(
            "Broadcasting auction extended: Auction={AuctionId}, NewEnd={NewEnd}, Count={Count}, Minutes={Minutes}",
            notification.AuctionId,
            notification.NewEndTime,
            notification.ExtensionCount,
            notification.ExtensionMinutes);
    }
}