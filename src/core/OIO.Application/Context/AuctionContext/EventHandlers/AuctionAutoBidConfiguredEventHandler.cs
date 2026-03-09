using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionAutoBidConfiguredEventHandler
    : INotificationHandler<AuctionAutoBidConfiguredEvent>
{
    private readonly ILogger<AuctionAutoBidConfiguredEventHandler> _logger;

    public AuctionAutoBidConfiguredEventHandler(
        ILogger<AuctionAutoBidConfiguredEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(AuctionAutoBidConfiguredEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "🤖 AutoBid configured. Auction={AuctionId}, Bidder={BidderId}, MaxAmount={MaxAmount}.",
            notification.AuctionId, notification.BidderId, notification.MaxAmount);

        return Task.CompletedTask;
    }
}