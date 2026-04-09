namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record AuctionDetailDto(
    AuctionDto Auction,
    ItemDto Item,
    IReadOnlyList<BidDto> RecentBids,
    IReadOnlyList<PriceHistoryDto> PriceHistory,
    ParticipantInfoDto? CurrentUserParticipant = null,
    CurrentUserBidStateDto? CurrentUserBidState = null,
    CurrentBuyerOrderDto? CurrentBuyerOrder = null,
    SealedBidInfoDto? SealedBidInfo = null);

public sealed record SealedBidInfoDto(
    int SealedBidCount,
    bool CurrentUserHasSubmittedSealedBid,
    string? CurrentUserSealedBidStatus);

public sealed record CurrentBuyerOrderDto(
    Guid OrderId,
    string OrderStatus,
    bool CanPayNow);
