using MediatR;
using Microsoft.Extensions.Logging;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class AuctionCreatedEventHandler
    : INotificationHandler<AuctionCreatedEvent>
{
    private readonly ILogger<AuctionCreatedEventHandler> _logger;

    public AuctionCreatedEventHandler(ILogger<AuctionCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(AuctionCreatedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "📦 Auction created. Id={AuctionId}, Item={ItemId}, Seller={SellerId}, StartingPrice={Price}.",
            notification.AuctionId, notification.ItemId,
            notification.SellerId, notification.StartingPrice);

        return Task.CompletedTask;

        // Phase 2: thêm auto-assign to admin review queue
        // Phase 2: thêm notification to seller "Auction created, pending publish"
    }
}