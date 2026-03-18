using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.ModerationContext.Aggregates;
using OIO.Domain.Context.ModerationContext.Enums;
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
    private readonly IAuctionNotificationService _notificationService;
    private readonly ILogger<BidPlacedEventHandler> _logger;

    public BidPlacedEventHandler(
        IDbContext dbContext,
        IUnitOfWork unitOfWork,
        IRuntimeSettings runtimeSettings,
        IAuctionNotificationService notificationService,
        ILogger<BidPlacedEventHandler> logger)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _runtimeSettings = runtimeSettings;
        _notificationService = notificationService;
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
                .Include(a => a.Bids),
            cancellationToken: ct);

        if (auction is null)
        {
            _logger.LogWarning(
                "Skipped bid broadcast because auction {AuctionId} could not be loaded.",
                notification.AuctionId);
            return;
        }

        var bidder = await _dbContext.GetByIdAsync<User, UserId>(
            bidderId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(u => u.Profile),
            cancellationToken: ct);

        _logger.LogInformation(
            "Broadcasting bid: Auction={AuctionId}, Bidder={BidderId}, Amount={Amount}",
            notification.AuctionId,
            notification.BidderId,
            notification.Amount);

        await _notificationService.NotifyBidPlacedAsync(
            Guid.Parse(notification.AuctionId),
            new BidNotification(
                AuctionId: Guid.Parse(notification.AuctionId),
                BidId: Guid.Parse(notification.BidId),
                BidderId: Guid.Parse(notification.BidderId),
                BidderDisplayName: AuctionNotificationDisplayNames.Resolve(bidder),
                Amount: notification.Amount,
                CurrentPrice: auction.Pricing.CurrentAmount,
                MinimumNextBid: auction.GetMinimumBidAmount().Amount,
                TotalBids: auction.BidCount,
                IsAutoBid: notification.IsAutoBid,
                Timestamp: new DateTimeOffset(
                    DateTime.SpecifyKind(notification.BidTime, DateTimeKind.Utc))),
            ct);

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
