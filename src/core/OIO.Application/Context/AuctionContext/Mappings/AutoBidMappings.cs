using OIO.Application.Context.AuctionContext.DTOs;
using OIO.Domain.Context.AuctionContext.Aggregates.Auctions;

namespace OIO.Application.Context.AuctionContext.Mappings;

public static class AutoBidMappings
{
    public static AutoBidDto ToDto(this AutoBid autoBid)
    {
        return new AutoBidDto(
            Id: autoBid.Id.Value,
            AuctionId: autoBid.AuctionId.Value,
            BidderId: autoBid.BidderId.Value,
            IsEnabled: autoBid.IsEnabled,
            MaxAmount: autoBid.MaxAmount.Amount,
            CurrentAmount: autoBid.CurrentAmount.Amount,
            IncrementAmount: autoBid.IncrementAmount?.Amount,
            Status: autoBid.Status.Id,
            TotalAutoBids: autoBid.TotalAutoBids,
            LastAutoBidAt: autoBid.LastAutoBidAt,
            CreatedAt: autoBid.CreatedAt);
    }
}