namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record MyBidDto(
    Guid AuctionId,
    Guid ItemId,
    string ItemTitle,
    string? PrimaryImageUrl,
    string AuctionStatus,
    MoneyDto CurrentPrice,
    MoneyDto MyLatestBidAmount,
    string Position,
    DateTime? WonAt,
    DateTime LastBidAt,
    int BidCountForUser);