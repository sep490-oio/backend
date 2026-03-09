using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.UserContext.Queries.GetPermissions;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public sealed class GetPermissionsEndpoint : IEndpoint
{
    public sealed record Parameters : PermissionFilterParameters;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetPermissions, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetPermissionsQuery(parameters);

                var result = await sender.Send(query, ct);

                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadPermissions)
            .WithName(ApiEndpoint.Names.Admins.GetPermissions)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}