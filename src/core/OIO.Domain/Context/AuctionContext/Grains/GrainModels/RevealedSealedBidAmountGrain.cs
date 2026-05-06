using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;

namespace OIO.Domain.Context.AuctionContext.Grains.GrainModels;

// Sent across the Orleans grain boundary as part of
// IAuctionGrain.EndAuctionAsync(..., IReadOnlyCollection<RevealedSealedBidAmountGrain>, ...).
// Orleans needs an explicit codec to deep-copy this record between calls;
// without [GenerateSerializer] + [property: Id(n)] on each positional parameter,
// runtime fails with CodecNotFoundException for the type.
[GenerateSerializer]
[Alias("OIO.Domain.Context.AuctionContext.Grains.GrainModels.RevealedSealedBidAmountGrain")]
public sealed record RevealedSealedBidAmountGrain(
    [property: Id(0)] Guid SealedBidId,
    [property: Id(1)] MoneyGrain Amount)
{
    public static RevealedSealedBidAmountGrain From(RevealedSealedBidAmount revealedSealedBidAmount)
    {
        return new RevealedSealedBidAmountGrain(
            revealedSealedBidAmount.SealedBidId.Value,
            MoneyGrain.From(revealedSealedBidAmount.Amount));
    }
}
