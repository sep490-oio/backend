using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.GetCurrentUser;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class GetCurrentUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetCurrentUser, async (ISender sender, CancellationToken ct) =>
            {
                var query = new GetCurrentUserQuery();
                
                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Me.Read)
            .WithName(ApiEndpoint.Names.Me.GetMe)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}