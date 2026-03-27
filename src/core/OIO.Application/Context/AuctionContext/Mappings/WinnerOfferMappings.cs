using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

namespace OIO.Application.Context.AuctionContext.Mappings;

internal static class WinnerOfferMappings
{
    public static WinnerOfferDto ToDto(this AuctionWinnerOffer offer)
    {
        return new WinnerOfferDto(
            OfferId: offer.Id.Value,
            AuctionId: offer.AuctionId.Value,
            AuctionTitle: offer.Auction.Item.Title.Value,
            OfferAmount: offer.Auction.Pricing.CurrentAmount,
            Currency: offer.Auction.Pricing.Currency.Id,
            Status: offer.OfferStatus.Id,
            ExpiresAt: offer.ExpiresAt,
            CreatedAt: offer.OfferedAt);
    }
}
