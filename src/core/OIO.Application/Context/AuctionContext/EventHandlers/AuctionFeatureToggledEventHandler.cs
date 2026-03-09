using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionFeatureToggledEventHandler
    : INotificationHandler<AuctionFeatureToggledEvent>
{
    private readonly ILogger<AuctionFeatureToggledEventHandler> _logger;

    public AuctionFeatureToggledEventHandler(ILogger<AuctionFeatureToggledEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(AuctionFeatureToggledEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "⭐ Auction feature toggled. Auction={AuctionId}, IsFeatured={IsFeatured}.",
            notification.AuctionId, notification.IsFeatured);

        return Task.CompletedTask;
    }
}