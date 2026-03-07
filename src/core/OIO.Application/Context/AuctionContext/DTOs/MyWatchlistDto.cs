namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record MyAuctionWatchlistDto(
    Guid AuctionId,
    string ItemTitle,
    string? PrimaryImageUrl,
    decimal CurrentPrice,
    string Currency,
    string AuctionStatus,
    int BidCount,
    DateTime EndTime,
    TimeSpan RemainingTime,
    bool NotifyOnBid,
    bool NotifyOnEnd,
    DateTime WatchedAt);