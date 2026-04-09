namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record PlaceBidResultDto(
    BidDto Bid,
    int AutoBidsCascaded,
    decimal FinalPrice,
    bool WasImmediatelyOutbid,
    bool TriggeredBuyNowCap = false,
    Guid? OrderId = null);
