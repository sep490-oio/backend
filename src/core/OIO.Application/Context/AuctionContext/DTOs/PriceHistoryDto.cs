namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record PriceHistoryDto(
    MoneyDto Price,
    string Type,
    Guid? BidId,
    DateTime RecordedAt,
    string? BidderDisplayName = null);
