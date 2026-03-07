using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.UserContext.Queries.GetLoginHistory;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class GetLoginHistoryEndpoint : IEndpoint
{
    public sealed record Parameters : PagedParameters;
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetLoginHistory, async (
                [AsParameters] Parameters parameters,
                ISender sender,
                CancellationToken ct = default) =>
                {
                    var query = new GetLoginHistoryQuery(parameters);
                    
                    var result = await sender.Send(query, ct);

                    return result.ToOkHttpResult();
                })
            .RequireAuthorization()
            .WithName(ApiEndpoint.Names.Me.GetMyLoginHistory)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}