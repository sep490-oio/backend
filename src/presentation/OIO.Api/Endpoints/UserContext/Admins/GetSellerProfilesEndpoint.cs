using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.GetSellerProfiles;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class GetSellerProfilesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetSellerProfiles, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetSellerProfilesQuery();

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadSellerProfiles)
            .WithName(ApiEndpoint.Names.Admins.GetSellerProfiles)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}
