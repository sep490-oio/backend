namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record PriceHistoryDto(
    MoneyDto Price,
    Guid? BidId,
    DateTime RecordedAt);