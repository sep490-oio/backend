using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.GetUserById;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public class GetUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetUser, async (Guid userId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetUserByIdQuery(userId), ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadUsers)
            .WithName(ApiEndpoint.Names.Admins.GetUserById)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}