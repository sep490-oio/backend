using MediatR;
using OIO.Api.Common;
using OIO.Application.Context.AdminContext.Queries.GetAllSettings;

namespace OIO.Api.Endpoints.UserContext.Admins;

public sealed class GetAllSettingsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Admins.GetAllSettings, async (
                ISender sender,
                CancellationToken ct) =>
            {
                var query = new GetAllSettingsQuery();
                
                var result = await sender.Send(query, ct);
                
                return result.ToOkHttpResult();
            })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Admins.GetAllSettings)
            .WithTags(ApiEndpoint.Tags.Admins);
    }
}