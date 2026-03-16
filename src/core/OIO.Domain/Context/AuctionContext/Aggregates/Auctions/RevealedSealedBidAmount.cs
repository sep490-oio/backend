using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.Shared.ValueObjects;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed record RevealedSealedBidAmount(
    SealedBidId SealedBidId,
    Money Amount);
