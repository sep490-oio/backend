using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.GetMySellerProfile;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class GetMySellerProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetMySellerProfile, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetMySellerProfileQuery();

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadSellerProfile)
            .WithName(ApiEndpoint.Names.Me.GetMySellerProfile)
            .WithTags(ApiEndpoint.Tags.SellerProfiles);
    }
}
