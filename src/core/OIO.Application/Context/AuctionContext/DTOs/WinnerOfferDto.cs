namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record WinnerOfferDto(
    Guid OfferId,
    Guid AuctionId,
    string AuctionTitle,
    decimal OfferAmount,
    string Currency,
    string Status,
    DateTime? ExpiresAt,
    DateTime CreatedAt);
