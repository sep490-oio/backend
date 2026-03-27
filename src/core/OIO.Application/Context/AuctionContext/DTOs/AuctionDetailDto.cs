namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record AuctionDetailDto(
    AuctionDto Auction,
    ItemDto Item,
    IReadOnlyList<BidDto> RecentBids,
    IReadOnlyList<PriceHistoryDto> PriceHistory,
    ParticipantInfoDto? CurrentUserParticipant = null);