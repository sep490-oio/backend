using MediatR;
using OIO.Api.Common;
using OIO.Api.Extensions;
using OIO.Application.Abstractions.Commons;
using OIO.Application.UserContext.Queries.GetUserAddresses;

namespace OIO.Api.Endpoints.Users;

public class GetAddressesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/users/me/addresses", async ([AsParameters] PagedParameters pagedParameters, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new GetUserAddressesQuery(pagedParameters), ct);

                return result.IsFailure ? result.Error.ProcessError() : Results.Ok(result.Value);
            })
            .RequireAuthorization()
            .WithTags(Tags.Users);
    }
}