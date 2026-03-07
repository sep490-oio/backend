using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.GetUserProfile;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class GetCurrentUserProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetCurrentUserProfile, async (ISender sender, CancellationToken ct) =>
            {
                var query = new GetUserProfileQuery();
                
                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.GetMyProfile)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}