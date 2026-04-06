namespace OIO.Application.Context.AuctionContext.Services;

/// <summary>
/// Publishes auto-bid state changes to individual users via UserHub.
/// Called directly after commit for instant delivery.
/// </summary>
public interface IAutoBidRealtimePublisher
{
    /// <summary>Publish state change for a specific bidder's auto-bid on an auction.</summary>
    Task PublishAsync(Guid auctionId, Guid bidderId, CancellationToken ct = default);

    /// <summary>Publish state changes for ALL auto-bidders on an auction (after cascade).</summary>
    Task PublishAllForAuctionAsync(Guid auctionId, CancellationToken ct = default);
}
