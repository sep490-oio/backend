using OIO.Application.Abstractions.Sorting;
using OIO.Application.Context.UserContext.DTOs;
using OIO.Domain.Context.ReviewContext.Aggregates;
using OIO.Domain.Context.UserContext.Aggregates.Users;

namespace OIO.Application.Context.UserContext.Mappings;

public static class SellerProfileMappings
{
    public static SellerProfileDto ToDto(this SellerProfile p, SellerRatingSummary? rating = null)
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
            AverageRating: rating?.AverageRating ?? 0m,
            RatingCount: rating?.TotalReviews ?? 0,
            CreatedAt: p.CreatedAt,
            ModifiedAt: p.ModifiedAt);
    }

    public static PublicSellerProfileDto ToPublicDto(this SellerProfile p, SellerRatingSummary? rating = null)
    {
        return new PublicSellerProfileDto(
            Id: p.Id.Value,
            StoreName: p.StoreName,
            StoreDescription: p.StoreDescription,
            Status: p.Status.Id,
            TotalSalesCount: p.TotalSalesCount,
            TrustScore: p.TrustScoreOverall,
            AverageRating: rating?.AverageRating ?? 0m,
            RatingCount: rating?.TotalReviews ?? 0,
            CreatedAt: p.CreatedAt);
    }
    
    public static readonly SortMappingDefinition PublicSellerProfileDtoSortMapping =
        SortMappingBuilder<PublicSellerProfileDto, SellerProfile>.Create()
            .Map(dto => dto.TrustScore, entity => entity.TrustScoreOverall)
            .Map(dto => dto.TotalSalesCount, entity => entity.TotalSalesCount)
            .Map(dto => dto.StoreName, entity => entity.StoreName)
            .Map(dto => dto.CreatedAt, entity => entity.CreatedAt)
            .Build();
}
