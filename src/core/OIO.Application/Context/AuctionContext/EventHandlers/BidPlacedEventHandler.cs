using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Clock;
using OIO.Application.Abstractions.Media;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.Aggregates.Items.Events;

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
                NewHighAmount: notification.NewHighAmount,
                MinimumNextBid: 0,
                NewHighBidderDisplayName: ""),
            ct);
    }
}

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

internal sealed class AuctionExtendedEventHandler
    : INotificationHandler<AuctionExtendedEvent>
{
    private readonly IAuctionNotificationService _notificationService;
    private readonly ILogger<AuctionExtendedEventHandler> _logger;

    public AuctionExtendedEventHandler(
        IAuctionNotificationService notificationService,
        ILogger<AuctionExtendedEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(AuctionExtendedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Broadcasting auction extended: Auction={AuctionId}, NewEnd={NewEnd}",
            notification.AuctionId, notification.NewEndTime);

        await _notificationService.NotifyAuctionExtendedAsync(
            Guid.Parse(notification.AuctionId),
            new AuctionExtendedNotification(
                AuctionId: Guid.Parse(notification.AuctionId),
                NewEndTime: notification.NewEndTime,
                ExtensionMinutes: notification.ExtensionMinutes),
            ct);
    }
}

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

internal sealed class MediaRemovedFromItemEventHandler
    : INotificationHandler<MediaRemovedFromItemEvent>
{
    private readonly IMediaSignatureService _mediaService;
    private readonly ILogger<MediaRemovedFromItemEventHandler> _logger;

    public MediaRemovedFromItemEventHandler(
        IMediaSignatureService mediaService,
        ILogger<MediaRemovedFromItemEventHandler> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task Handle(
        MediaRemovedFromItemEvent notification, CancellationToken ct)
    {
        var resourceType = UploadContextRegistry.ParseResourceType(
            notification.ResourceType);

        var deleted = await _mediaService.DeleteResourceAsync(
            notification.PublicId, resourceType, ct);

        if (deleted)
        {
            _logger.LogInformation(
                "Deleted {Type} resource from storage: {PublicId} (Item={ItemId})",
                notification.ResourceType, notification.PublicId, notification.ItemId);
        }
        else
        {
            _logger.LogWarning(
                "Failed to delete {Type} resource from storage: {PublicId} (Item={ItemId}). " +
                "May need manual cleanup.",
                notification.ResourceType, notification.PublicId, notification.ItemId);
        }
    }
}