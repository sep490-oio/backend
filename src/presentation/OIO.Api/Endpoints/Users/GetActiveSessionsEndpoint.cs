using System.ComponentModel.DataAnnotations;
using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Abstractions.Commons;
using OIO.Application.UserContext.Queries.GetActiveSessions;

namespace OIO.Api.Endpoints.Users;

public class GetActiveSessionsEndpoint : IEndpoint
{
    public sealed record Parameters : PagedParameters
    {
        public Guid? DeviceId { get; init; }
    }
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/users/me/sessions",
                async (
                    [AsParameters] Parameters parameters,
                    ISender sender, 
                    CancellationToken ct) =>
                {
                    var query = new GetActiveSessionsQuery(parameters.DeviceId, parameters);
                    
                    var result = await sender.Send(query, ct);

                    return result.IsFailure ? result.Error.ProcessError() : Results.Ok(result.Value);
                })
            .RequireAuthorization()
            .WithTags(Tags.Users);
    }
}