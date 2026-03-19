namespace OIO.Application.Context.UserContext.DTOs;

public sealed record SellerProfileDto(
    Guid Id,
    string StoreName,
    string StoreDescription,
    string Status,
    DateTime? VerifiedAt,
    int TotalSalesCount,
    decimal TotalSalesAmount,
    decimal TrustScore,
    DateTime? TrustScoreCalculatedAt,
    DateTime CreatedAt,
    DateTime? ModifiedAt);

public sealed record PublicSellerProfileDto(
    Guid Id,
    string StoreName,
    string StoreDescription,
    string Status,
    int TotalSalesCount,
    decimal TrustScore,
    DateTime CreatedAt);
