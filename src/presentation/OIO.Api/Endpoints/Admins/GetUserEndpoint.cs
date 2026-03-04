using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Queries.GetUserById;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Admins;

public class GetUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetUser, async (Guid userId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetUserByIdQuery(userId), ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Users.Read)
            .WithName(ApiEndpoint.Names.Admins.GetUser)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}