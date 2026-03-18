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
            MaxAmount: autoBid.Budget.MaxPrice.ToDto(),
            CurrentAmount: autoBid.Budget.CurrentPrice.ToDto(),
            ReservedAmount: autoBid.Budget.ReservedPrice.ToDto(),
            IncrementAmount: autoBid.Budget.Increment?.ToDto(),
            Status: autoBid.Status.Id,
            TotalAutoBids: autoBid.TotalAutoBids,
            LastAutoBidAt: autoBid.LastAutoBidAt,
            StopReason: autoBid.StopReason,
            StoppedAt: autoBid.StoppedAt,
            LastValidationAt: autoBid.LastValidationAt,
            CreatedAt: autoBid.CreatedAt);
    }
}
