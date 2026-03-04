using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.UserContext.Queries.GetPermissions;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Admins;

public class GetPermissionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetPermissions, async (
                [AsParameters] PagedParameters parameters,
                ISender sender,
                CancellationToken ct) =>
            {
                var result = await sender.Send(new GetPermissionsQuery(parameters), ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Permissions.Read)
            .WithName(ApiEndpoint.Names.Admins.GetPermissions)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}