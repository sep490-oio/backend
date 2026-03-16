namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record PublicSellerItemAuctionSummaryDto(
    Guid AuctionId,
    string AuctionStatus,
    string AuctionType,
    decimal CurrentPrice,
    string Currency,
    DateTime? StartTime,
    DateTime? EndTime);

public sealed record PublicSellerItemDto(
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
    PublicSellerItemAuctionSummaryDto? Auction,
    bool HasLiveAuction);
