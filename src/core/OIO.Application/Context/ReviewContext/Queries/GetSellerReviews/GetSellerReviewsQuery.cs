using OIO.Application.Abstractions.Commons;
using OIO.Application.Abstractions.Messaging;
using OIO.Application.Context.ReviewContext.DTOs;
using OIO.Domain.SeedWork.Checks.Extensions;
using OIO.Domain.SeedWork.Errors;

namespace OIO.Application.Context.ReviewContext.Queries.GetSellerReviews;

public sealed record GetSellerReviewsQuery(
    Guid SellerId,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedList<SellerReviewDto>>, IHasValidate
{
    public ViolationsError Validate() =>
        GetSellerReviewsQuery.Check()
            .WithOwnerName("GetSellerReviews")
            .Field(SellerId).NotEmptyGuid();
}
