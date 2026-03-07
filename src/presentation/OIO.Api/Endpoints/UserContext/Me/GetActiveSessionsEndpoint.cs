using MediatR;
using OIO.Api.Common;
using OIO.Application.Abstractions.Commons;
using OIO.Application.Context.UserContext.Queries.GetActiveSessions;
using OIO.Domain.AppDefinitions;

namespace OIO.Api.Endpoints.UserContext.Me;

public class GetActiveSessionsEndpoint : IEndpoint
{
    public sealed record Parameters : PagedParameters
    {
        public Guid? DeviceId { get; init; }
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.Me.GetActiveSessions,
                async (
                    [AsParameters] Parameters parameters,
                    ISender sender, 
                    CancellationToken ct) =>
                {
                    var query = new GetActiveSessionsQuery(parameters.DeviceId, parameters);
                    
                    var result = await sender.Send(query, ct);

                    return result.ToOkHttpResult();
                })
            .RequireAuthorization(App.Permissions.Catalogs.Me.ReadSessions)
            .WithName(ApiEndpoint.Names.Me.GetActiveSessions)
            .WithTags(ApiEndpoint.Tags.Me);
    }
}