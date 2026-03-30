using CSharpFunctionalExtensions;
using Microsoft.EntityFrameworkCore;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Data;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ReviewContext.DTOs;
using OIO.Application.Extensions;
using OIO.Domain.Context.ReviewContext.Aggregates;
using OIO.Domain.Context.UserContext.Aggregates.Users;
using OIO.Domain.Context.UserContext.ValueObjects.Ids;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ReviewContext.Queries.GetSellerReviews;

internal sealed class GetSellerReviewsQueryHandler(
    IDbContext dbContext)
    : IQueryHandler<GetSellerReviewsQuery, PagedList<SellerReviewDto>>
{
    public async Task<Result<PagedList<SellerReviewDto>, Error>> Handle(
        GetSellerReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var parameters =  request.Parameters;
        var sellerId = UserId.From(request.SellerId);

        var query = dbContext.Set<SellerReview>()
            .AsNoTracking()
            .Where(r => r.SellerId == sellerId)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var reviews = await query
            
            .Select(r => new
            {
                Review = r,
                ReviewerName = dbContext.Set<User>()
                    .Where(u => u.Id == r.ReviewerId)
                    .Select(u => u.UserName!.Value)
                    .FirstOrDefault() ?? string.Empty
            })
            .Select(x => new SellerReviewDto(
                x.Review.Id.Value,
                x.Review.OrderId.Value,
                x.Review.ReviewerId.Value,
                x.ReviewerName,
                x.Review.SellerId.Value,
                x.Review.OverallRating.Value,
                x.Review.CommunicationRating != null ? (int?)x.Review.CommunicationRating.Value : null,
                x.Review.ShippingSpeedRating != null ? (int?)x.Review.ShippingSpeedRating.Value : null,
                x.Review.ItemAccuracyRating != null ? (int?)x.Review.ItemAccuracyRating.Value : null,
                x.Review.Title,
                x.Review.Comment,
                x.Review.CreatedAt))
            .ToPagedListAsync(totalCount, parameters, cancellationToken);

        return reviews;
    }
}
