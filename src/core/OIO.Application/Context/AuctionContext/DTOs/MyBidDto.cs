namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record MyBidDto(
    Guid Id,
    Guid AuctionId,
    string ItemTitle,
    string? PrimaryImageUrl,
    MoneyDto Amount,
    MoneyDto CurrentPrice,
    string Status,
    string AuctionStatus,
    bool IsHighestBid,
    DateTime BidPlacedAt,
    DateTime? AuctionEndTime);