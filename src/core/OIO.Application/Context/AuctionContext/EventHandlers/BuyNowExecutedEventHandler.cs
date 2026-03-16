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

internal sealed class AuctionBuyNowReservedEventHandler
    : INotificationHandler<AuctionBuyNowReservedEvent>
{
    private readonly IAuctionNotificationService _notificationService;

    public AuctionBuyNowReservedEventHandler(IAuctionNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public Task Handle(AuctionBuyNowReservedEvent notification, CancellationToken ct)
    {
        return _notificationService.NotifyBuyNowReservedAsync(
            Guid.Parse(notification.AuctionId),
            new BuyNowReservedNotification(
                AuctionId: Guid.Parse(notification.AuctionId),
                ReservationId: Guid.Parse(notification.ReservationId),
                BuyerId: Guid.Parse(notification.BuyerId),
                BuyNowPrice: notification.BuyNowPrice,
                DepositAppliedAmount: notification.DepositAppliedAmount,
                AmountDue: notification.AmountDue,
                ExpiresAt: new DateTimeOffset(notification.ExpiresAt)),
            ct);
    }
}

internal sealed class AuctionBuyNowReservationReleasedEventHandler
    : INotificationHandler<AuctionBuyNowReservationReleasedEvent>
{
    private readonly IAuctionNotificationService _notificationService;

    public AuctionBuyNowReservationReleasedEventHandler(IAuctionNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public Task Handle(AuctionBuyNowReservationReleasedEvent notification, CancellationToken ct)
    {
        return _notificationService.NotifyBuyNowReservationReleasedAsync(
            Guid.Parse(notification.AuctionId),
            new BuyNowReservationReleasedNotification(
                AuctionId: Guid.Parse(notification.AuctionId),
                ReservationId: Guid.Parse(notification.ReservationId),
                BuyerId: Guid.Parse(notification.BuyerId),
                Reason: notification.Reason,
                ReleasedAt: new DateTimeOffset(notification.OccurredAt)),
            ct);
    }
}
