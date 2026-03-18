namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record SealedBidDto(
    Guid Id,
    Guid AuctionId,
    Guid BidderId,
    string AmountEncrypted,
    string Status,
    DateTime CreatedAt,
    DateTime? RevealedAt,
    Guid? RevealedBy,
    decimal? RevealedAmount);
