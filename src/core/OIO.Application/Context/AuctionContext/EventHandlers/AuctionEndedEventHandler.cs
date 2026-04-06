using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OIO.Application.Abstractions.Data;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionEndedEventHandler
    : INotificationHandler<AuctionEndedEvent>
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<AuctionEndedEventHandler> _logger;

    public AuctionEndedEventHandler(
        IDbContext dbContext,
        ILogger<AuctionEndedEventHandler> logger)
    {
        _dbContext = dbContext;
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

        _logger.LogInformation(
            "Broadcasting auction ended: Auction={AuctionId}, Winner={WinnerId}",
            notification.AuctionId, notification.WinnerId);
    }
}
