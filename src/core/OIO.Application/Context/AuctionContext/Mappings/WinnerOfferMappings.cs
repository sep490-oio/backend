using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

namespace OIO.Application.Context.AuctionContext.Mappings;

internal static class WinnerOfferMappings
{
    public static WinnerOfferDto ToDto(this AuctionWinnerOffer offer)
    {
        return new WinnerOfferDto(
            Id: offer.Id.Value,
            AuctionId: offer.AuctionId.Value,
            UserId: offer.UserId.Value,
            RankNo: offer.RankNo,
            Status: offer.OfferStatus.Id,
            OfferedAt: offer.OfferedAt,
            ExpiresAt: offer.ExpiresAt,
            RespondedAt: offer.RespondedAt);
    }
}
