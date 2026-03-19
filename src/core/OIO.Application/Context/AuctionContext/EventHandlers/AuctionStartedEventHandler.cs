using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Application.Context.NotificationContext;
using OIO.Application.Context.NotificationContext.Commands.CreateNotification;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.NotificationContext.Enums;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionStartedEventHandler
    : INotificationHandler<AuctionStartedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ISender _sender;
    private readonly IAuctionNotificationService _notificationService;
    private readonly ILogger<AuctionStartedEventHandler> _logger;

    public AuctionStartedEventHandler(
        IDbContext dbContext,
        ISender sender,
        IAuctionNotificationService notificationService,
        ILogger<AuctionStartedEventHandler> logger)
    {
        _dbContext = dbContext;
        _sender = sender;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(AuctionStartedEvent notification, CancellationToken ct)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            auctionId,
            queryBuilder: query => query
                .AsNoTracking()
                .Include(a => a.Item)
                .Include(a => a.Participants),
            cancellationToken: ct);

        if (auction?.Info is null)
        {
            _logger.LogWarning(
                "Skipped auction-started broadcast because auction {AuctionId} could not be loaded with timing info.",
                notification.AuctionId);
            return;
        }

        // SignalR broadcast (existing)
        await _notificationService.NotifyAuctionStartedAsync(
            auction.Id.Value,
            new AuctionStartedNotification(
                AuctionId: auction.Id.Value,
                StartTime: auction.Info.StartTime,
                EndTime: auction.Info.EndTime),
            ct);

        // Persistent notifications for qualified participants
        var participantUserIds = auction.Participants
            .Where(p => p.UserId != auction.Item.SellerId)
            .Select(p => p.UserId.Value)
            .Distinct()
            .ToList();

        foreach (var userId in participantUserIds)
        {
            await NotificationDispatch.DispatchAsync(
                _sender, _logger,
                new CreateNotificationCommand(
                    UserId: userId,
                    NotificationType: "auction",
                    EventType: "auction_started",
                    Title: "Phien dau gia da bat dau",
                    Message: $"Phien dau gia \"{auction.Item.Title.Value}\" da bat dau. Hay tham gia dau gia!",
                    Priority: NotificationPriority.Normal,
                    EntityType: "Auction",
                    EntityId: auctionId.Value,
                    Metadata: NotificationDispatch.SerializeMetadata(new
                    {
                        auctionId = auctionId.Value,
                        startTime = auction.Info.StartTime,
                        endTime = auction.Info.EndTime
                    })),
                ct);
        }

        _logger.LogInformation(
            "Auction started: Auction={AuctionId}, Start={StartTime}, End={EndTime}. Notified={Count} participants.",
            notification.AuctionId,
            auction.Info.StartTime,
            auction.Info.EndTime,
            participantUserIds.Count);
    }
}
