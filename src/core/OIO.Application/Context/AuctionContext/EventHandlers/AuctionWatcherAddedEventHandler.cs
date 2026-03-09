using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionWatcherAddedEventHandler
    : INotificationHandler<AuctionWatcherAddedEvent>
{
    private readonly ILogger<AuctionWatcherAddedEventHandler> _logger;

    public AuctionWatcherAddedEventHandler(ILogger<AuctionWatcherAddedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(AuctionWatcherAddedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Watcher added. Auction={AuctionId}, User={UserId}.",
            notification.AuctionId, notification.UserId);

        return Task.CompletedTask;
    }
}