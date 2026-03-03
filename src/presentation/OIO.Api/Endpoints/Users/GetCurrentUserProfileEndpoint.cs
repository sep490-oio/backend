using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Queries.GetUserProfile;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Users;

public class GetCurrentUserProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Users.GetCurrentUserProfile, async (ISender sender, CancellationToken ct) =>
            {
                var query = new GetUserProfileQuery();
                
                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Users.ReadMe)
            .WithName(ApiEndpoint.Names.Users.GetCurrentUserProfile)
            .WithTags(ApiEndpoint.Tags.Users);
    }
}