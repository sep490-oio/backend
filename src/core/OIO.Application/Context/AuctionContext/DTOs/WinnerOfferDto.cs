namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record WinnerOfferDto(
    Guid Id,
    Guid AuctionId,
    Guid UserId,
    int RankNo,
    string Status,
    DateTime OfferedAt,
    DateTime? ExpiresAt,
    DateTime? RespondedAt);
