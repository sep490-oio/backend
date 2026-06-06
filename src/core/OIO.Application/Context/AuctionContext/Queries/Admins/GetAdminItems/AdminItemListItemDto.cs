using OIO.Application.Context.AuctionContext.DTOs;

namespace OIO.Application.Context.AuctionContext.Queries.Admins.GetAdminItems;

public sealed record AdminItemListItemDto(
    Guid Id,
    Guid SellerId,
    string SellerDisplayName,
    Guid? CategoryId,
    string Title,
    string Condition,
    string Status,
    string PrimaryImageUrl,
    DateTime CreatedAt,
    string? CurrentPhysicalLocation,
    Guid? CurrentAuctionId,
    string? CurrentAuctionStatus,
    int TotalAuctions
);
