using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.GetAllRoles;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public sealed class GetAllRolesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetRoles, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetAllRolesQuery();

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Admins.GetRoles)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}