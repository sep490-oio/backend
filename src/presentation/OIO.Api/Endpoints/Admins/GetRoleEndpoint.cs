using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Queries.GetRoles;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Admins;

public class GetRolesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetRoles, async (ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetRolesQuery(), ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Roles.TogglePermission)
            .WithName(ApiEndpoint.Names.Admins.GetRoles)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}