using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Abstractions.Commons;
using OIO.Application.UserContext.Queries.GetUserAddresses;

namespace OIO.Api.Endpoints.Users;

public class GetAddressesEndpoint : IEndpoint
{
    public sealed record Parameters : PagedParameters;
    
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/users/me/addresses", async ([AsParameters] Parameters parameters, ISender sender, CancellationToken ct) =>
            {
                var query = new GetUserAddressesQuery(parameters);
                
                var result = await sender.Send(query, ct);

                return result.IsFailure ? result.Error.ProcessError() : Results.Ok(result.Value);
            })
            .RequireAuthorization()
            .WithTags(Tags.Users);
    }
}