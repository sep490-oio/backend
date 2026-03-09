using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AuctionContext.Queries.GetAllActiveCategories;

namespace OIO.Api.Endpoints.AuctionContext.Categories;

public sealed class GetAllCategoriesEndpoint : IEndpoint
{
    public sealed record Parameters : GetAllActiveCategoriresFilterParameters;
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Categories.GetAll, async (
                ISender sender,
                [AsParameters] Parameters parameters,
                CancellationToken ct) =>
            {
                var query = new GetAllActiveCategoriesQuery(parameters);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Categories.GetAllCategories)
            .WithTags(ApiEndpoint.Tags.Categories)
            .Produces(StatusCodes.Status200OK);
    }
}