using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Queries.GetUserById;

namespace OIO.Api.Endpoints.Admins;

public class GetUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetUser, async (Guid userId, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetUserByIdQuery(userId), ct);

                return result.ToNoContentHttpResult();
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.Admins.GetUser)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}