using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetCategoryById;

namespace OIO.Api.Endpoints.AuctionContext.Categories;

public sealed class GetCategoryByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Categories.GetById, async (
                Guid categoryId,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetCategoryByIdQuery(categoryId);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Categories.GetCategoryById)
            .WithTags(ApiEndpoint.Tags.Categories)
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}