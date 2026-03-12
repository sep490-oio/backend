using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.GetPublicSellerProfile;

namespace OIO.Api.Endpoints.UserContext.Sellers;

public class GetSellerByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Sellers.GetById, async (
                Guid sellerId,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetPublicSellerProfileQuery(sellerId);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Sellers.GetSellerById)
            .WithTags(ApiEndpoint.Tags.Sellers);
    }
}
