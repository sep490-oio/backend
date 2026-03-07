namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record MyBidDto(
    Guid BidId,
    Guid AuctionId,
    string ItemTitle,
    string? PrimaryImageUrl,
    decimal BidAmount,
    decimal CurrentPrice,
    string BidStatus,
    string AuctionStatus,
    bool IsHighestBid,
    DateTime BidPlacedAt,
    DateTime AuctionEndTime);