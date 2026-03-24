namespace OIO.Application.Context.AuctionContext.DTOs;

public sealed record PublicItemDto(
    Guid Id,
    Guid SellerId,
    string SellerName,
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
