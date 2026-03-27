using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.GetSellers;

namespace OIO.Api.Endpoints.UserContext.Sellers;

public sealed class GetSellersEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Sellers.GetAll, async (
                [AsParameters] GetSellersFilterParameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetSellersQuery(parameters);
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Sellers.GetSellers)
            .WithTags(ApiEndpoint.Tags.Sellers)
            .Produces(StatusCodes.Status200OK);
    }
}
