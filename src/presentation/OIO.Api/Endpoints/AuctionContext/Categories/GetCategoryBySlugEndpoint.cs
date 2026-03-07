using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetCategoryBySlug;

namespace OIO.Api.Endpoints.AuctionContext.Categories;

public sealed class GetCategoryBySlugEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Categories.GetBySlug, async (
                string slug,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetCategoryBySlugQuery(slug);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Categories.GetCategoryBySlug)
            .WithTags(ApiEndpoint.Tags.Categories)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}