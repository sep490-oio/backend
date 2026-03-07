using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AdminContext.Queries.GetSettingByKey;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Admins;

public sealed class GetSettingByKeyEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetSettingByKey, async (
                string key,
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetSettingByKeyQuery(key);
                var result = await sender.Send(query, ct);
                return result.ToOkHttpResult();
            })
            .RequireAuthorization(App.Permissions.Catalogs.Admin.ReadSettings)
            .WithName(ApiEndpoint.Names.Admins.GetSettingByKey)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}