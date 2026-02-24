using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Abstractions.Commons;
using OIO.Application.UserContext.Queries.GetActiveSessions;

namespace OIO.Api.Endpoints.Users;

public class GetActiveSessionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/users/me/sessions",
                async (
                    Guid currentDeviceId, 
                    [AsParameters] PagedParameters pagedParameters,
                    ISender sender, 
                    CancellationToken ct) =>
                {
                    var result = await sender.Send(new GetActiveSessionsQuery(currentDeviceId, pagedParameters), ct);

                    return result.IsFailure ? result.Error.ProcessError() : Results.Ok(result.Value);
                })
            .RequireAuthorization()
            .WithTags(Tags.Users);
    }
}