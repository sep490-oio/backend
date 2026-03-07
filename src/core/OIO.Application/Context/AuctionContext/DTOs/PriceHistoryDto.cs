namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record PriceHistoryDto(
    decimal Price,
    Guid? BidId,
    DateTime RecordedAt);