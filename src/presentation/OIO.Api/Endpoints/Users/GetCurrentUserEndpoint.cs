using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.UserContext.Queries.GetCurrentUser;

namespace OIO.Api.Endpoints.Users;

public class GetCurrentUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/users/me", async (ISender sender, CancellationToken ct) =>
            {
                var query = new GetCurrentUserQuery();
                
                var result = await sender.Send(query, ct);

                return result.IsFailure ? result.Error.ProcessError() : Results.Ok(result.Value);
            })
            .RequireAuthorization()
            .WithTags(Tags.Users);
    }
}