namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record CurrentUserBidStateDto(
    string Position,
    bool IsCurrentWinner,
    Guid? LatestBidId,
    decimal? LatestBidAmount,
    string? LatestBidStatus,
    DateTime? LatestBidAt,
    bool HasAutoBid,
    string? AutoBidStatus);
