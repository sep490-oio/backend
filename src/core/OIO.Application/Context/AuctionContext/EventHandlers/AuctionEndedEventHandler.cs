using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Application.Context.AuctionContext.Hubs;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionEndedEventHandler
    : INotificationHandler<AuctionEndedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly IAuctionNotificationService _notificationService;
    private readonly ILogger<AuctionEndedEventHandler> _logger;

    public AuctionEndedEventHandler(
        IDbContext dbContext,
        IAuctionNotificationService notificationService,
        ILogger<AuctionEndedEventHandler> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(AuctionEndedEvent notification, CancellationToken ct)
    {
        var auctionId = AuctionId.From(Guid.Parse(notification.AuctionId));
        var auction = await _dbContext.GetByIdAsync<Auction, AuctionId>(
            auctionId,
            queryBuilder: query => query.AsNoTracking(),
            cancellationToken: ct);

        if (auction is null)
        {
            _logger.LogWarning(
                "Skipped generic auction-ended broadcast because auction {AuctionId} could not be loaded.",
                notification.AuctionId);
            return;
        }

        if (auction.Status != AuctionStatus.Ended)
        {
            _logger.LogDebug(
                "Skipped generic auction-ended broadcast for auction {AuctionId} because status is {Status}.",
                notification.AuctionId,
                auction.Status.Id);
            return;
        }

        User? winner = null;
        if (auction.WinnerId is not null)
        {
            var winnerId = auction.WinnerId.Value;
            winner = await _dbContext.GetByIdAsync<User, UserId>(
                winnerId,
                queryBuilder: query => query
                    .AsNoTracking()
                    .Include(u => u.Profile),
                cancellationToken: ct);
        }

        _logger.LogInformation(
            "Broadcasting auction ended: Auction={AuctionId}, Winner={WinnerId}",
            notification.AuctionId, notification.WinnerId);

        await _notificationService.NotifyAuctionEndedAsync(
            Guid.Parse(notification.AuctionId),
            new AuctionEndedNotification(
                AuctionId: Guid.Parse(notification.AuctionId),
                WinnerId: notification.WinnerId != null ? Guid.Parse(notification.WinnerId) : null,
                WinnerDisplayName: AuctionNotificationDisplayNames.Resolve(winner),
                FinalPrice: notification.FinalPrice,
                TotalBids: notification.TotalBids,
                ReserveMet: notification.ReserveMet),
            ct);
    }
}
