using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Queries.GetCurrentUser;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Users;

public class GetCurrentUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Users.GetCurrentUser, async (ISender sender, CancellationToken ct) =>
            {
                var query = new GetCurrentUserQuery();
                
                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Users.ReadMe)
            .WithName(ApiEndpoint.Names.Users.GetCurrentUser)
            .WithTags(ApiEndpoint.Tags.Users);
    }
}