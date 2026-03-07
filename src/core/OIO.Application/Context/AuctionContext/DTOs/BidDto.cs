namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record BidDto(
    Guid Id,
    Guid AuctionId,
    Guid BidderId,
    decimal Amount,
    bool IsAutoBid,
    string Status,
    DateTime CreatedAt);