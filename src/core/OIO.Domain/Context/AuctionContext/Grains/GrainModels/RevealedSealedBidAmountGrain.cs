using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;
using OIO.Domain.Context.AuctionContext.Grains.GrainValueObjects;

namespace OIO.Domain.Context.AuctionContext.Grains.GrainModels;

public sealed record RevealedSealedBidAmountGrain(
    Guid SealedBidId,
    MoneyGrain Amount)
{
    public static RevealedSealedBidAmountGrain From(RevealedSealedBidAmount revealedSealedBidAmount)
    {
        return new RevealedSealedBidAmountGrain(
            revealedSealedBidAmount.SealedBidId.Value,
            MoneyGrain.From(revealedSealedBidAmount.Amount));
    }
}
