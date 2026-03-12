using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;

namespace OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

public sealed class AuctionWinnerOffer : BaseEntity<AuctionWinnerOfferId>
{
    public AuctionId AuctionId { get; private set; }
    public UserId UserId { get; private set; }
    public int RankNo { get; private set; }
    public WinnerOfferStatus OfferStatus { get; private set; }
    public DateTime OfferedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }

    // Navigation
    public Auction Auction { get; private set; } = null!;

    private AuctionWinnerOffer() { }
}