using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

namespace OIO.Application.Context.AuctionContext.Mappings;

public static class AuctionPriceHistoryMappings
{
    public static PriceHistoryDto ToDto(this AuctionPriceHistory ph)
    {
        return new PriceHistoryDto(
            Price: ph.Price.ToDto(),
            Type: ph.Type.Id,
            BidId: ph.BidId?.Value,
            RecordedAt: ph.CreatedAt);
    }
}
