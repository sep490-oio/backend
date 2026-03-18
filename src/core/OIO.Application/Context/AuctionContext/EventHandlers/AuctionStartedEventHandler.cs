using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionStartedEventHandler
    : INotificationHandler<AuctionStartedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IAuctionNotificationService _notificationService;
    private readonly ILogger<AuctionStartedEventHandler> _logger;

    public AuctionStartedEventHandler(
        IDbContext dbContext,
        IAuctionNotificationService notificationService,
        ILogger<AuctionStartedEventHandler> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(AuctionStartedEvent notification, CancellationToken ct)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            auctionId,
            queryBuilder: query => query.AsNoTracking(),
            cancellationToken: ct);

        if (auction?.Info is null)
        {
            _logger.LogWarning(
                "Skipped auction-started broadcast because auction {AuctionId} could not be loaded with timing info.",
                notification.AuctionId);
            return;
        }

        await _notificationService.NotifyAuctionStartedAsync(
            auction.Id.Value,
            new AuctionStartedNotification(
                AuctionId: auction.Id.Value,
                StartTime: auction.Info.StartTime,
                EndTime: auction.Info.EndTime),
            ct);

        _logger.LogInformation(
            "Broadcasting auction started: Auction={AuctionId}, Start={StartTime}, End={EndTime}",
            notification.AuctionId,
            auction.Info.StartTime,
            auction.Info.EndTime);
    }
}
