using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetAllCategories;

namespace OIO.Api.Endpoints.AuctionContext.Categories;

public sealed class GetAllCategoriesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Categories.GetAll, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetAllCategoriesQuery();

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Categories.GetAllCategories)
            .WithTags(ApiEndpoint.Tags.Categories)
            .Produces(StatusCodes.Status200OK);
    }
}