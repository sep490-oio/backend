using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Abstractions.Commons;
using OIO.Application.UserContext.Queries.GetLoginHistory;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.Users;

public class GetLoginHistoryEndpoint : IEndpoint
{
    public sealed record Parameters : PagedParameters;
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Users.GetLoginHistory, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct = default) =>
                {
                    var query = new GetLoginHistoryQuery(parameters);
                    
                    var result = await sender.Send(query, ct);

                    return result.ToOkHttpResult();
                })
            .RequireAuthorization(App.Permissions.Catalogs.Users.ReadLoginHistory)
            .WithName(ApiEndpoint.Names.Users.GetLoginHistory)
            .WithTags(ApiEndpoint.Tags.Users);
    }
}