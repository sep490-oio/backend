using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Application.Context.NotificationContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class OutbidEventHandler
    : INotificationHandler<OutbidEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IAuctionNotificationService _notificationService;
    private readonly ISender _sender;
    private readonly ILogger<OutbidEventHandler> _logger;

    public OutbidEventHandler(
        IDbContext dbContext,
        IAuctionNotificationService notificationService,
        ISender sender,
        ILogger<OutbidEventHandler> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(OutbidEvent notification, CancellationToken ct)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            auctionId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(a => a.Bids),
            cancellationToken: ct);

        if (auction is null)
        {
            _logger.LogWarning(
                "Skipped outbid notification because auction {AuctionId} could not be loaded.",
                notification.AuctionId);
            return;
        }

        var highBidderId = auction.GetCurrentWinningBid()?.BidderId ??
                           UserId.From(Guid.Parse(notification.NewHighBidderId));

        var highBidder = await _dbContext.GetByIdAsync<User, UserId>(
            highBidderId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(u => u.Profile),
            cancellationToken: ct);

        _logger.LogInformation(
            "Notifying outbid: Auction={AuctionId}, OutbidUser={OutbidUser}",
            notification.AuctionId, notification.OutbidBidderId);

        await _notificationService.NotifyOutbidAsync(
            Guid.Parse(notification.OutbidBidderId),
            new OutbidNotification(
                AuctionId: Guid.Parse(notification.AuctionId),
                NewHighAmount: auction.Pricing.CurrentAmount,
                MinimumNextBid: auction.GetMinimumBidAmount().Amount,
                NewHighBidderDisplayName: AuctionNotificationDisplayNames.Resolve(highBidder)),
            ct);

        // Create persistent bell notification for outbid bidder
        var itemTitle = auction.Item?.Title?.Value ?? "Auction";
        await NotificationDispatch.DispatchAsync(
            _sender,
            _logger,
            new CreateNotificationCommand(
                UserId: Guid.Parse(notification.OutbidBidderId),
                NotificationType: "auction",
                EventType: "auction_outbid",
                Title: "Ban da bi vuot gia!",
                Message: $"Phien dau gia \"{itemTitle}\" da co gia moi: {auction.Pricing.CurrentAmount}. Dat gia cao hon de gianh lai.",
                Priority: NotificationPriority.High,
                EntityType: "Auction",
                EntityId: auctionId.Value,
                Metadata: NotificationDispatch.SerializeMetadata(new
                {
                    auctionId = auctionId.Value,
                    newHighAmount = auction.Pricing.CurrentAmount,
                    minimumNextBid = auction.GetMinimumBidAmount().Amount,
                    currency = auction.Pricing.Currency.Id
                })),
            ct);
    }
}
