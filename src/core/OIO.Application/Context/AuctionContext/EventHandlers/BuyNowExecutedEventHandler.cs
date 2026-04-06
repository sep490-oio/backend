using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class BuyNowExecutedEventHandler
    : INotificationHandler<BuyNowExecutedEvent>
{
    private readonly ILogger<BuyNowExecutedEventHandler> _logger;

    public BuyNowExecutedEventHandler(
        ILogger<BuyNowExecutedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(BuyNowExecutedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Broadcasting buy now: Auction={AuctionId}, Buyer={BuyerId}",
            notification.AuctionId, notification.BuyerId);

        return Task.CompletedTask;
    }
}
