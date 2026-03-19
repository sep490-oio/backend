using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Application.Context.UserContext.Mappings;

public static class SellerProfileMappings
{
    public static SellerProfileDto ToDto(this SellerProfile p)
    {
        return new SellerProfileDto(
            Id: p.Id.Value,
            StoreName: p.StoreName,
            StoreDescription: p.StoreDescription,
            Status: p.Status.Id,
            VerifiedAt: p.VerifiedAt,
            TotalSalesCount: p.TotalSalesCount,
            TotalSalesAmount: p.TotalSalesAmount,
            TrustScore: p.TrustScoreOverall,
            TrustScoreCalculatedAt: p.TrustScoreCalculatedAt,
            CreatedAt: p.CreatedAt,
            ModifiedAt: p.ModifiedAt);
    }

    public static PublicSellerProfileDto ToPublicDto(this SellerProfile p)
    {
        return new PublicSellerProfileDto(
            Id: p.Id.Value,
            StoreName: p.StoreName,
            StoreDescription: p.StoreDescription,
            Status: p.Status.Id,
            TotalSalesCount: p.TotalSalesCount,
            TrustScore: p.TrustScoreOverall,
            CreatedAt: p.CreatedAt);
    }
}
