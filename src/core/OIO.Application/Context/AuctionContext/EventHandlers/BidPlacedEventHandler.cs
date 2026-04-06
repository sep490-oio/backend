using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.Enums;
using OIO.Domain.Context.NotificationContext.Enums;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class BidPlacedEventHandler
    : INotificationHandler<BidPlacedEvent>
{
    private static readonly TimeSpan BidBurstWindow = TimeSpan.FromMinutes(5);

    private readonly IDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRuntimeSettings _runtimeSettings;
    private readonly ISender _sender;
    private readonly ILogger<BidPlacedEventHandler> _logger;

    public BidPlacedEventHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IRuntimeSettings runtimeSettings,
        ISender sender,
        ILogger<BidPlacedEventHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _runtimeSettings = runtimeSettings;
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(BidPlacedEvent notification, CancellationToken ct)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));
        var bidderId = UserId.From(Guid.Parse(notification.BidderId));

        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            auctionId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(a => a.Bids)
                .Include(a => a.Watchers)
                .Include(a => a.Item),
            cancellationToken: ct);

        if (auction is null)
        {
            _logger.LogWarning(
                "Skipped bid broadcast because auction {AuctionId} could not be loaded.",
                notification.AuctionId);
            return;
        }

        _logger.LogInformation(
            "Broadcasting bid: Auction={AuctionId}, Bidder={BidderId}, Amount={Amount}",
            notification.AuctionId,
            notification.BidderId,
            notification.Amount);

        // Notify watchers with NotifyOnBid enabled
        var sellerId = auction.Item?.SellerId;
        var watcherUserIds = auction.Watchers
            .Where(w => w.NotifyOnBid && w.UserId != bidderId && (sellerId == null || w.UserId != sellerId))
            .Select(w => w.UserId.Value)
            .Distinct()
            .ToList();

        var itemTitle = auction.Item?.Title?.Value ?? "Auction";
        foreach (var watcherUserId in watcherUserIds)
        {
            await NotificationDispatch.DispatchAsync(
                _sender,
                _logger,
                new CreateNotificationCommand(
                    UserId: watcherUserId,
                    NotificationType: "auction",
                    EventType: "auction_bid_placed",
                    Title: "Gia moi tren phien dau gia ban theo doi",
                    Message: $"Phien dau gia \"{itemTitle}\" da co gia moi: {notification.Amount}.",
                    Priority: NotificationPriority.Normal,
                    EntityType: "Auction",
                    EntityId: auctionId.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        auctionId = auctionId.Value,
                        bidAmount = notification.Amount,
                        currentPrice = auction.Pricing.CurrentAmount,
                        currency = auction.Pricing.Currency.Id
                    })),
                ct);
        }

        await CreateBidBurstAlertIfNeededAsync(notification, ct);
    }

    private async Task CreateBidBurstAlertIfNeededAsync(BidPlacedEvent notification, CancellationToken ct)
    {
        var threshold = _runtimeSettings.Monitoring.BidBurstThreshold;

        if (threshold <= 0)
            return;

        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));
        var windowStart = notification.BidTime - BidBurstWindow;

        var recentBidCount = await _dbContext.Set<Bid>()
            .AsNoTracking()
            .CountAsync(
                x => x.AuctionId == auctionId && x.CreatedAt >= windowStart,
                ct);

        if (recentBidCount < threshold)
            return;

        var hasOpenAlert = await _dbContext.Set<MonitoringAlert>()
            .AsNoTracking()
            .AnyAsync(
                x => x.EntityType == "Auction" &&
                     x.EntityId == auctionId.Value &&
                     x.AlertType == "bid_burst" &&
                     x.Status == AlertStatus.Open &&
                     x.CreatedAt >= windowStart,
                ct);

        if (hasOpenAlert)
            return;

        _dbContext.Insert(MonitoringAlert.Create(
            entityType: "Auction",
            entityId: auctionId.Value,
            alertType: "bid_burst",
            severity: AlertSeverity.Medium,
            payload: System.Text.Json.JsonSerializer.Serialize(new
            {
                auctionId = auctionId.Value,
                bidId = notification.BidId,
                bidderId = notification.BidderId,
                bidCount = recentBidCount,
                windowMinutes = BidBurstWindow.TotalMinutes
            }),
            nowUtc: notification.OccurredAt));

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
