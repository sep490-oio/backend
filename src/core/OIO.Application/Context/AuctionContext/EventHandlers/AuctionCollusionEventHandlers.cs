using MediatR;
using OIO.Application.Context.AuctionContext.Services;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions.Events;

namespace OIO.Application.Context.AuctionContext.EventHandlers;

internal sealed class BidPlacedCollusionDetectionEventHandler(
    IAuctionCollusionDetectionService collusionDetectionService)
    : INotificationHandler<BidPlacedEvent>
{
    public Task Handle(BidPlacedEvent notification, CancellationToken cancellationToken)
    {
        return collusionDetectionService.DetectAfterBidPlacedAsync(notification, cancellationToken);
    }
}

internal sealed class AuctionBuyNowReservedCollusionDetectionEventHandler(
    IAuctionCollusionDetectionService collusionDetectionService)
    : INotificationHandler<AuctionBuyNowReservedEvent>
{
    public Task Handle(AuctionBuyNowReservedEvent notification, CancellationToken cancellationToken)
    {
        return collusionDetectionService.DetectAfterBuyNowReservedAsync(notification, cancellationToken);
    }
}
