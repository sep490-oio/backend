using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetCategoryChildren;

namespace OIO.Api.Endpoints.AuctionContext.Categories;

public sealed class GetCategoryChildrenEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Categories.GetChildren, async (
                Guid categoryId,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetCategoryChildrenQuery(categoryId);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Categories.GetCategoryChildren)
            .WithTags(ApiEndpoint.Tags.Categories)
            .Produces(StatusCodes.Status200OK);
    }
}