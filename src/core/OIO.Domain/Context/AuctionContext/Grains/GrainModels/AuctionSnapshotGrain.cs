namespace OIO.Domain.Context.AuctionContext.Grains.GrainModels;

[GenerateSerializer]
[Alias("OIO.Domain.Context.AuctionContext.Grains.GrainModels.AuctionSnapshotGrain")]
public sealed record AuctionSnapshotGrain(
    Guid AuctionId,
    decimal CurrentPrice,
    decimal MinimumNextBid,
    int BidCount,
    string Status,
    DateTime EndTime,
    Guid? WinnerId);