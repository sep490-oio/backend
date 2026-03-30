using MediatR;
using Microsoft.AspNetCore.Mvc;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.ReviewContext.DTOs;
using OIO.Application.Context.ReviewContext.Queries.GetSellerReviews;

namespace OIO.Api.Endpoints.ReviewContext;

public sealed class GetSellerReviewsEndpoint : IEndpoint
{
    public sealed record Parameters : GetSellerReviewsFilter;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Reviews.GetBySeller, async (
                Guid sellerId,
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetSellerReviewsQuery(
                    sellerId,
                    parameters);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Reviews.GetSellerReviews)
            .WithTags(ApiEndpoint.Tags.Reviews)
            .Produces<PagedList<SellerReviewDto>>(StatusCodes.Status200OK);
    }
}
