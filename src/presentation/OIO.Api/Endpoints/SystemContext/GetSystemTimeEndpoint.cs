using OIO.Api.Common;
using OIO.Application.Abstractions.Clock;

namespace OIO.Api.Endpoints.SystemContext;

public sealed class GetSystemTimeEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoint.Url.System.GetTime, (IClock clock) =>
            {
                var now = clock.UtcNow;
                return Results.Ok(new { ServerTime = now });
            })
            .AllowAnonymous()
            .WithName(ApiEndpoint.Names.System.GetSystemTime)
            .WithTags(ApiEndpoint.Tags.System)
            .Produces(StatusCodes.Status200OK);
    }
}
