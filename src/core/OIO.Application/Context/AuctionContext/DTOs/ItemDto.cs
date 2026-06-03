namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record ItemDto(
    Guid Id,
    Guid SellerId,
    Guid? CategoryId,
    string Title,
    string? Description,
    string Condition,
    string Status,
    int Quantity,
    IReadOnlyList<ItemMediaDto> Images,
    DateTime CreatedAt,
    bool HasInboundShipment = false,
    bool HasLiveAuction = false,
    Guid? WarehouseItemId = null,
    ItemAuctionSummaryDto? Auction = null);

public sealed record ItemAuctionSummaryDto(
    Guid AuctionId,
    string AuctionStatus,
    string AuctionType,
    decimal CurrentPrice,
    string Currency,
    DateTimeOffset? StartTime,
    DateTimeOffset? EndTime);