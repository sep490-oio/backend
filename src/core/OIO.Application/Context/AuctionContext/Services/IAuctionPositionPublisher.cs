namespace OIO.Application.Context.AuctionContext.Services;

/// <summary>
/// Publishes bidder position changes to individual users via UserHub.
/// Position: leading, outbid, won, lost.
/// Called directly after commit for instant delivery.
/// </summary>
public interface IAuctionPositionPublisher
{
    /// <summary>Publish position for a specific bidder on an auction.</summary>
    Task PublishAsync(Guid auctionId, Guid bidderId, CancellationToken ct = default);

    /// <summary>Publish position for ALL bidders on an auction (after auction end/sold/failed).</summary>
    Task PublishForAllBiddersAsync(Guid auctionId, CancellationToken ct = default);
}
