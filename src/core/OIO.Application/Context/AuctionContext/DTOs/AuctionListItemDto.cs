namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record AuctionListItemDto(
    Guid Id,
    string ItemTitle,
    string? PrimaryImageUrl,
    decimal CurrentPrice,
    decimal StartingPrice,
    decimal? BuyNowPrice,
    string Currency,
    string Status,
    int BidCount,
    int WatchCount,
    DateTime StartTime,
    DateTime EndTime,
    TimeSpan RemainingTime,
    bool IsEndingSoon,
    bool IsFeatured,
    Guid SellerId);