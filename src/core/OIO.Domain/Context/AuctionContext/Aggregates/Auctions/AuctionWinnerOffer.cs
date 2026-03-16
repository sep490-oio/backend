using CSharpFunctionalExtensions;
using OIO.Domain.Context.AuctionContext.Enums;
using OIO.Domain.Context.AuctionContext.ValueObjects.Ids;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Entities;
using OIO.Domain.SeedWork.Errors;

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

    public Auction Auction { get; private set; } = null!;

    private AuctionWinnerOffer() { }

    public static AuctionWinnerOffer Create(
        AuctionId auctionId,
        UserId userId,
        int rankNo,
        DateTime offeredAt,
        DateTime expiresAt)
    {
        return new AuctionWinnerOffer
        {
            Id = AuctionWinnerOfferId.From(Guid.CreateVersion7()),
            AuctionId = auctionId,
            UserId = userId,
            RankNo = rankNo,
            OfferStatus = WinnerOfferStatus.Pending,
            OfferedAt = offeredAt,
            ExpiresAt = expiresAt
        };
    }

    public bool IsActiveAt(DateTime nowUtc) =>
        OfferStatus == WinnerOfferStatus.Pending &&
        (!ExpiresAt.HasValue || ExpiresAt.Value >= nowUtc);

    public UnitResult<Error> Accept(DateTime nowUtc)
    {
        if (OfferStatus != WinnerOfferStatus.Pending)
            return Error.Conflict("AuctionWinnerOffer.InvalidState", "Offer cannot be accepted in its current state.");

        if (ExpiresAt.HasValue && ExpiresAt.Value < nowUtc)
            return Error.Conflict("AuctionWinnerOffer.Expired", "Offer has already expired.");

        OfferStatus = WinnerOfferStatus.Accepted;
        RespondedAt = nowUtc;
        return UnitResult.Success<Error>();
    }

    public UnitResult<Error> Decline(DateTime nowUtc)
    {
        if (OfferStatus != WinnerOfferStatus.Pending)
            return Error.Conflict("AuctionWinnerOffer.InvalidState", "Offer cannot be declined in its current state.");

        OfferStatus = WinnerOfferStatus.Declined;
        RespondedAt = nowUtc;
        return UnitResult.Success<Error>();
    }

    public void Expire(DateTime nowUtc)
    {
        if (OfferStatus != WinnerOfferStatus.Pending)
            return;

        OfferStatus = WinnerOfferStatus.Expired;
        RespondedAt = nowUtc;
    }

    public void Cancel(DateTime nowUtc)
    {
        if (OfferStatus != WinnerOfferStatus.Pending)
            return;

        OfferStatus = WinnerOfferStatus.Cancelled;
        RespondedAt = nowUtc;
    }
}
