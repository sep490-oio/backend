namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record AutoBidDto(
    Guid Id,
    Guid AuctionId,
    Guid BidderId,
    bool IsEnabled,
    decimal MaxAmount,
    decimal CurrentAmount,
    decimal? IncrementAmount,
    string Status,
    int TotalAutoBids,
    DateTime? LastAutoBidAt,
    DateTime CreatedAt);